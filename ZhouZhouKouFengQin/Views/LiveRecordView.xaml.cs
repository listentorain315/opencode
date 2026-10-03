using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GameMusicPlayer.Models;
using GameMusicPlayer.Services;

namespace GameMusicPlayer.Views
{
    public partial class LiveRecordView : UserControl
    {
        private const double GapThresholdMs = 60;
        private const double TimelineScale = 0.06;

        private enum RecState { Idle, Armed, Recording, Paused, Finished }

        private sealed class RecNote
        {
            public string Key = "";
            public bool IsSharp;
            public bool IsFlat;
            public bool IsNatural;
            public double StartMs;
            public int Duration;
            public bool Active;
        }

        private sealed class NoteRow : INotifyPropertyChanged
        {
            private readonly RecNote _note;
            private readonly double _startMs;

            public NoteRow(RecNote note, int index)
            {
                _note = note;
                _startMs = note.StartMs;
                RowIndex = index - 1;
                IndexText = index.ToString("D2");
            }

            public int RowIndex { get; }
            public string IndexText { get; }
            public string KeyText => string.IsNullOrEmpty(_note.Key) ? "休止" : _note.Key;
            public string TypeText
            {
                get
                {
                    if (string.IsNullOrEmpty(_note.Key)) return "停顿";
                    bool l = _note.IsFlat, r = _note.IsSharp, m = _note.IsNatural;
                    if (m && r) return "♯ 升半音";
                    if (m && l) return "♭ 降半音";
                    if (r && !l) return "↑ 升八度";
                    if (l && !r) return "↓ 降八度";
                    if (m) return "中键";
                    return "普通";
                }
            }
            public string StartText => FormatMs(_startMs);

            private string _durationText = "";
            public string DurationText
            {
                get => _durationText;
                set { if (_durationText != value) { _durationText = value; OnPropertyChanged(nameof(DurationText)); } }
            }

            private string _stateText = "";
            public string StateText
            {
                get => _stateText;
                set { if (_stateText != value) { _stateText = value; OnPropertyChanged(nameof(StateText)); } }
            }

            public void Refresh(double nowMs)
            {
                if (_note.Active)
                {
                    DurationText = $"{Math.Max(0, (int)(nowMs - _note.StartMs))} ms";
                    StateText = "● 录制中";
                }
                else
                {
                    DurationText = $"{_note.Duration} ms";
                    StateText = "已录入";
                }
            }

            public static string FormatMs(double ms)
            {
                int total = Math.Max(0, (int)ms);
                return $"{total / 60000:D2}:{total % 60000 / 1000:D2}.{total % 1000 / 10:D2}";
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static readonly Dictionary<Key, string> KeyMap = new Dictionary<Key, string>
        {
            { Key.Z, "z" }, { Key.X, "x" }, { Key.C, "c" }, { Key.V, "v" },
            { Key.B, "b" }, { Key.N, "n" }, { Key.M, "m" }, { Key.OemComma, "," },
            { Key.D1, "1" }, { Key.D2, "2" }, { Key.D3, "3" }, { Key.D4, "4" },
            { Key.D5, "5" }, { Key.D6, "6" }, { Key.D7, "7" },
            { Key.NumPad1, "1" }, { Key.NumPad2, "2" }, { Key.NumPad3, "3" }, { Key.NumPad4, "4" },
            { Key.NumPad5, "5" }, { Key.NumPad6, "6" }, { Key.NumPad7, "7" },
        };

        private static readonly Brush TileBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x1A, 0x24));
        private static readonly Brush TileActiveBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xD3, 0xEE));
        private static readonly Brush TileTextBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));
        private static readonly Brush TileTextActiveBrush = new SolidColorBrush(Color.FromRgb(0x06, 0x20, 0x28));
        private static readonly Brush NoteNormalBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xD3, 0xEE));
        private static readonly Brush NoteSharpBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xA3, 0xFF));
        private static readonly Brush NoteFlatBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB0, 0x20));
        private static readonly Brush NoteNaturalBrush = new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA));
        private static readonly Brush NoteRestBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x4A));
        private static readonly Brush PreviewCursorBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB0, 0x20));

        private readonly MainWindow _shell;
        private readonly SongStorage _songStorage = new SongStorage();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Dictionary<Key, RecNote> _activeNotes = new Dictionary<Key, RecNote>();
        private readonly List<RecNote> _notes = new List<RecNote>();
        private readonly ObservableCollection<NoteRow> _rows = new ObservableCollection<NoteRow>();
        private readonly Dictionary<string, Border> _keyTiles = new Dictionary<string, Border>();
        private readonly Dictionary<string, int> _tileRefs = new Dictionary<string, int>();
        private readonly Dictionary<Key, object> _monitorHandles = new Dictionary<Key, object>();
        private readonly List<string> _previewTiles = new List<string>();
        private readonly PreviewSoundService _sound = PreviewSoundService.Shared;
        private readonly SongPreviewPlayer _previewPlayer;

        private RecState _state = RecState.Idle;
        #region 全局输入捕获（鼠标任意位置 / 焦点被抢也能录）

        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
        private const int VkLButton = 0x01, VkRButton = 0x02, VkMButton = 0x04, VkControl = 0x11;

        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KbdLlHookStruct
        {
            public int VkCode;
            public int ScanCode;
            public int Flags;
            public int Time;
            public IntPtr DwExtraInfo;
        }

        private LowLevelKeyboardProc? _hookProc;   // 持引用防 GC
        private IntPtr _hookHandle = IntPtr.Zero;
        private readonly HashSet<Key> _hookDown = new HashSet<Key>();
        private bool HookActive => _hookHandle != IntPtr.Zero;

        private static bool IsHeld(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        private void InstallHook()
        {
            if (HookActive) return;
            _hookProc = HookCallback;
            _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookProc, GetModuleHandle(null), 0);
        }

        private void UninstallHook()
        {
            if (!HookActive) return;
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
            _hookProc = null;
            _hookDown.Clear();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                bool isDown = msg == WmKeyDown || msg == WmSysKeyDown;
                bool isUp = msg == WmKeyUp || msg == WmSysKeyUp;
                if (isDown || isUp)
                {
                    var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey(info.VkCode);
                    if (isDown)
                    {
                        bool repeat = !_hookDown.Add(key);
                        HandleCapturedKeyDown(key, repeat);
                    }
                    else
                    {
                        _hookDown.Remove(key);
                        HandleCapturedKeyUp(key);
                    }
                    UpdateModifierText();
                    // 不吞按键：继续传给焦点应用（游戏可实时跟弹）
                }
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        #endregion
        private bool _stickyFlat, _stickySharp, _stickyNatural;
        private int _quantizeStep = 25;

        private DispatcherTimer? _uiTimer;
        private double _displayTotalMs;
        private Line? _previewCursor;
        private bool _timelineDirty;

        public LiveRecordView(MainWindow shell)
        {
            InitializeComponent();

            _shell = shell;
            NotesListBox.ItemsSource = _rows;
            BuildKeyboard();
            UpdateModifierText();

            _previewPlayer = new SongPreviewPlayer(_sound);
            _previewPlayer.NoteStarted += PreviewPlayer_NoteStarted;
            _previewPlayer.NoteEnded += PreviewPlayer_NoteEnded;
            _previewPlayer.ProgressChanged += PreviewPlayer_ProgressChanged;
            _previewPlayer.PlaybackFinished += (s, e) => Dispatcher.Invoke(EndPreviewUi);
            _previewPlayer.PlaybackStopped += (s, e) => Dispatcher.Invoke(EndPreviewUi);

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();
            UpdateStatus();
        }

        public void Shutdown()
        {
            UninstallHook();
            _uiTimer?.Stop();
            _previewPlayer.Dispose();
        }

        #region 虚拟键盘

        private void BuildKeyboard()
        {
            KeyboardLowPanel.Children.Clear();
            KeyboardHighPanel.Children.Clear();
            _keyTiles.Clear();

            foreach (var key in new[] { "z", "x", "c", "v", "b", "n", "m", "," })
                KeyboardLowPanel.Children.Add(CreateKeyTile(key));
            foreach (var key in new[] { "1", "2", "3", "4", "5", "6", "7" })
                KeyboardHighPanel.Children.Add(CreateKeyTile(key));
        }

        private Border CreateKeyTile(string key)
        {
            var text = new TextBlock
            {
                Text = key.ToUpper(),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = 52,
                Height = 48,
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(10),
                Background = TileBrush,
                BorderBrush = (Brush)FindResource("StrokeBrush"),
                BorderThickness = new Thickness(1),
                Child = text
            };
            _keyTiles[key] = border;
            return border;
        }

        private void SetTileActive(string key, bool active)
        {
            if (!_keyTiles.TryGetValue(key, out var tile))
                return;
            tile.Background = active ? TileActiveBrush : TileBrush;
            if (tile.Child is TextBlock tb)
                tb.Foreground = active ? TileTextActiveBrush : TileTextBrush;
        }

        private void RetainTile(string key)
        {
            _tileRefs[key] = _tileRefs.TryGetValue(key, out int c) ? c + 1 : 1;
            SetTileActive(key, true);
        }

        private void ReleaseTile(string key)
        {
            if (!_tileRefs.TryGetValue(key, out int c))
                return;
            if (c <= 1)
            {
                _tileRefs.Remove(key);
                SetTileActive(key, false);
            }
            else
            {
                _tileRefs[key] = c - 1;
            }
        }

        #endregion

        #region 键盘 / 鼠标捕获（由 shell 路由）

        public bool ProcessKeyDown(KeyEventArgs e)
        {
            if (HookActive) return false;   // 全局钩子已接管，避免双路重复处理
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            return HandleCapturedKeyDown(key, e.IsRepeat);
        }

        private bool HandleCapturedKeyDown(Key key, bool isRepeat)
        {
            if (key == Key.F5) { RecordPause_Click(this, new RoutedEventArgs()); return true; }
            if (key == Key.F6) { Stop_Click(this, new RoutedEventArgs()); return true; }

            bool capturing = _state == RecState.Armed || _state == RecState.Recording;
            if (key == Key.Z && IsHeld(VkControl) && !capturing)
            {
                Undo_Click(this, new RoutedEventArgs());
                return true;
            }

            if (key == Key.LeftShift || key == Key.RightShift || key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt)
            {
                UpdateModifierText();
                return false;
            }

            if (IsEditing()) return false;
            if (!KeyMap.TryGetValue(key, out string? mapped)) return false;
            if (!capturing) return false;

            if (isRepeat || _activeNotes.ContainsKey(key)) return true;

            // 待弹奏 → 首个按键按下瞬间开始计时（时间轴 t=0）
            if (_state == RecState.Armed)
            {
                _stopwatch.Start();
                _state = RecState.Recording;
                UpdateStatus();
            }

            var mods = CurrentModifiers();
            double now = _stopwatch.Elapsed.TotalMilliseconds;
            var note = new RecNote
            {
                Key = mapped,
                IsSharp = mods.Sharp,
                IsFlat = mods.Flat,
                IsNatural = mods.Natural,
                StartMs = now,
                Duration = 0,
                Active = true
            };
            _activeNotes[key] = note;
            _notes.Add(note);
            RetainTile(mapped);
            AppendRow(note);          // 增量追加，避免整表重建造成按键延迟
            _timelineDirty = true;

            // 录入监听：按下即发声（松键后 450ms 尾音收尾）
            if (MonitorCheckBox.IsChecked == true)
            {
                if (_monitorHandles.TryGetValue(key, out var stale)) _sound.StopVoice(stale);
                var handle = _sound.PlayKey(mapped, mods.Sharp, mods.Flat, mods.Natural);
                if (handle != null) _monitorHandles[key] = handle;
            }
            return true;
        }

        public bool ProcessKeyUp(KeyEventArgs e)
        {
            if (HookActive) return false;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            return HandleCapturedKeyUp(key);
        }

        private bool HandleCapturedKeyUp(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift || key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt)
            {
                UpdateModifierText();
                return false;
            }

            if (_activeNotes.TryGetValue(key, out var note))
            {
                EndNote(key, note);
                return true;
            }
            return false;
        }

        private void EndNote(Key key, RecNote note)
        {
            double now = _stopwatch.Elapsed.TotalMilliseconds;
            note.Duration = Math.Max(1, (int)(now - note.StartMs));
            note.Active = false;
            _activeNotes.Remove(key);
            ReleaseTile(note.Key);
            if (_monitorHandles.TryGetValue(key, out var handle))
            {
                _monitorHandles.Remove(key);
                _sound.ScheduleStop(handle, PreviewSoundService.DefaultTailMs);
            }
            _timelineDirty = true;   // 行状态由 UiTimer 刷新，无需重建
        }

        public void OnShellDeactivated()
        {
            // 全局钩子能看到任意位置的 keyup：失焦不再终止音符，录制继续
            UpdateModifierText();
        }

        private bool IsCapturing => _state == RecState.Armed || _state == RecState.Recording;

        private void ModChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton chip) return;
            bool on = chip.IsChecked == true;
            switch (chip.Tag as string)
            {
                case "sharp": _stickySharp = on; break;
                case "flat": _stickyFlat = on; break;
                case "natural": _stickyNatural = on; break;
            }
            UpdateModifierText();
        }

        /// <summary>
        /// 当前修饰（鼠标键 + 屏幕芯片）：
        /// 左键=降八度 · 右键=升八度 · 中键=半音辅助（单独无效）
        /// 中键+左=降半音 · 中键+右=升半音
        /// </summary>
        private (bool Sharp, bool Flat, bool Natural) CurrentModifiers()
        {
            // 系统级查询鼠标键状态：鼠标在任意位置（含窗口外、其他窗口上）都生效
            bool sharp = _stickySharp || IsHeld(VkRButton);
            bool flat = _stickyFlat || IsHeld(VkLButton);
            bool natural = _stickyNatural || IsHeld(VkMButton);
            return (sharp, flat, natural);
        }

        private void UpdateModifierText()
        {
            var (sharp, flat, natural) = CurrentModifiers();
            ModifierText.Text =
                natural && sharp ? "当前: ♯ 升半音 (中键+右)" :
                natural && flat ? "当前: ♭ 降半音 (中键+左)" :
                sharp && !flat ? "当前: ↑ 升八度 (右键)" :
                flat && !sharp ? "当前: ↓ 降八度 (左键)" :
                natural ? "当前: 半音辅助 (需搭配左右键)" :
                "当前: 普通";
        }

        private static bool IsEditing()
        {
            return Keyboard.FocusedElement is TextBoxBase or PasswordBox;
        }

        private static bool IsOverInteractive(DependencyObject? source) => false;   // 不再过滤：任意位置修饰键都生效

        #endregion

        #region 录制状态机

        private void RecordPause_Click(object sender, RoutedEventArgs e)
        {
            switch (_state)
            {
                case RecState.Idle:
                case RecState.Paused:
                    ArmRecording();
                    break;
                case RecState.Armed:
                    _state = RecState.Idle;
                    UninstallHook();
                    UpdateStatus();
                    break;
                case RecState.Recording:
                    PauseRecording();
                    break;
                case RecState.Finished:
                    // 已结束待保存：须先保存或取消保存
                    break;
            }
        }

        private void ArmRecording()
        {
            if (_previewPlayer.IsPlaying) _previewPlayer.Stop();
            _shell.StopPlayerPreview();   // 其他操作：中断歌单试听
            _state = RecState.Armed;
            InstallHook();   // 待弹奏即接管全局键盘（含 F5/F6），焦点被抢也能录
            _shell.SetGlobalStatus("实时录制：等待首个按键…", true);
            UpdateStatus();
        }

        private void PauseRecording()
        {
            if (_state != RecState.Recording) return;
            FinalizeActiveNotes();
            _stopwatch.Stop();
            _state = RecState.Paused;
            UpdateStatus();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (_previewPlayer.IsPlaying) _previewPlayer.Stop();
            _sound.StopAll();          // 显式停止 = 立即全静音（不留任何尾音挂住）
            FinalizeActiveNotes();
            UninstallHook();
            if (_state == RecState.Idle || _state == RecState.Finished) return;

            _stopwatch.Stop();   // 结束录制即冻结计时
            if (_notes.Count == 0)
            {
                _stopwatch.Reset();
                _state = RecState.Idle;
                UiHub.Notify("没有可保存的音符");
            }
            else
            {
                _state = RecState.Finished;   // 待保存：右下角出现 保存/取消保存
                UiHub.Notify("录制已结束，可试听后保存或取消");
            }
            UpdateStatus();
        }

        private void FinalizeActiveNotes()
        {
            if (_activeNotes.Count == 0) return;
            double now = _stopwatch.Elapsed.TotalMilliseconds;
            foreach (var kv in _activeNotes.ToList())
            {
                kv.Value.Duration = Math.Max(1, (int)(now - kv.Value.StartMs));
                kv.Value.Active = false;
                ReleaseTile(kv.Value.Key);
                if (_monitorHandles.TryGetValue(kv.Key, out var handle))
                {
                    _sound.ScheduleStop(handle, PreviewSoundService.DefaultTailMs);
                }
            }
            _activeNotes.Clear();
            _monitorHandles.Clear();
            _timelineDirty = true;
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            double now = _stopwatch.Elapsed.TotalMilliseconds;
            TimerText.Text = NoteRow.FormatMs(now);
            UpdateModifierText();   // 修饰状态实时刷新（含鼠标在任意位置的按住状态）
            foreach (var row in _rows)
            {
                row.Refresh(now);
            }
            if (_timelineDirty)
            {
                _timelineDirty = false;
                RedrawTimeline();
            }
        }

        private void UpdateStatus()
        {
            switch (_state)
            {
                case RecState.Armed:
                    StatusText.Text = "待弹奏";
                    StatusDot.Fill = (Brush)FindResource("AmberBrush");
                    RecordButton.Content = "等待按键… (F5 取消)";
                    break;
                case RecState.Recording:
                    StatusText.Text = "录制中";
                    StatusDot.Fill = (Brush)FindResource("DangerBrush");
                    RecordButton.Content = "⏸ 暂停 (F5)";
                    break;
                case RecState.Paused:
                    StatusText.Text = "已暂停";
                    StatusDot.Fill = (Brush)FindResource("AmberBrush");
                    RecordButton.Content = "● 继续 (F5)";
                    break;
                case RecState.Finished:
                    StatusText.Text = "待保存";
                    StatusDot.Fill = (Brush)FindResource("AccentBrush");
                    RecordButton.Content = "● 开始录制 (F5)";
                    break;
                default:
                    StatusText.Text = _notes.Count > 0 ? "已停止" : "待机";
                    StatusDot.Fill = (Brush)FindResource("MutedBrush");
                    RecordButton.Content = "● 开始录制 (F5)";
                    break;
            }
            UpdateTransportUi();
        }

        /// <summary>按状态切换右下角保存区与传输条按钮可用性</summary>
        private void UpdateTransportUi()
        {
            bool finished = _state == RecState.Finished;
            SaveButton.Visibility = finished ? Visibility.Visible : Visibility.Collapsed;
            CancelSaveButton.Visibility = finished ? Visibility.Visible : Visibility.Collapsed;
            SaveHintText.Visibility = finished ? Visibility.Collapsed : Visibility.Visible;
            RecordButton.IsEnabled = !finished;
            StopButton.IsEnabled = !finished;
            ClearButton.IsEnabled = !finished;
        }

        #endregion

        #region 音符列表 / 时间轴

        private void AppendRow(RecNote note)
        {
            var row = new NoteRow(note, _rows.Count + 1);
            row.Refresh(_stopwatch.Elapsed.TotalMilliseconds);
            _rows.Add(row);
            CountText.Text = $"已录入 {_notes.Count} 个音符";
        }

        private void RefreshRows()
        {
            // 待保存态里删空了 → 自动回到待机
            if (_state == RecState.Finished && _notes.Count == 0)
            {
                _state = RecState.Idle;
                _stopwatch.Reset();
                TimerText.Text = NoteRow.FormatMs(0);
            }

            double now = _stopwatch.Elapsed.TotalMilliseconds;
            _rows.Clear();
            for (int i = 0; i < _notes.Count; i++)
            {
                var row = new NoteRow(_notes[i], i + 1);
                row.Refresh(now);
                _rows.Add(row);
            }
            CountText.Text = $"已录入 {_notes.Count} 个音符";
            RedrawTimeline();
            if (_state == RecState.Idle) UpdateStatus();
        }

        private void RedrawTimeline()
        {
            double? live = _activeNotes.Count > 0 ? _stopwatch.Elapsed.TotalMilliseconds : (double?)null;
            var notes = BuildSongNotes(live);

            TimelineCanvas.Children.Clear();
            double t = 0;
            foreach (var note in notes)
            {
                bool rest = string.IsNullOrEmpty(note.Key);
                double x = t * TimelineScale;
                double w = Math.Max(1, note.Duration * TimelineScale);
                var rect = new Rectangle
                {
                    Width = w,
                    Height = rest ? 10 : 40,
                    RadiusX = 4,
                    RadiusY = 4,
                    Fill = rest ? NoteRestBrush
                        : note.IsSharp ? NoteSharpBrush
                        : note.IsFlat ? NoteFlatBrush
                        : note.IsNatural ? NoteNaturalBrush
                        : NoteNormalBrush
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, rest ? 25 : 9);
                TimelineCanvas.Children.Add(rect);
                t += note.Duration;
            }

            _displayTotalMs = t;
            TimelineCanvas.Width = Math.Max(1, t * TimelineScale);

            if (_previewCursor != null)
            {
                TimelineCanvas.Children.Add(_previewCursor);
            }
        }

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null && int.TryParse(rb.Tag.ToString(), out int step))
            {
                _quantizeStep = step;
            }
            if (IsLoaded) RedrawTimeline();
        }

        private void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is int index)
            {
                if (index < 0 || index >= _notes.Count) return;
                if (_notes[index].Active) return;
                _notes.RemoveAt(index);
                RefreshRows();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_activeNotes.Count > 0)
            {
                FinalizeActiveNotes();
                return;
            }
            if (_notes.Count == 0) return;
            _notes.RemoveAt(_notes.Count - 1);
            RefreshRows();
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (_notes.Count == 0) return;
            bool ok = await UiHub.ConfirmAsync("确认清空", "确定清空所有已录制的音符吗？此操作不可恢复。");
            if (!ok) return;

            if (_previewPlayer.IsPlaying) _previewPlayer.Stop();
            _sound.StopAll();
            _monitorHandles.Clear();
            FinalizeActiveNotes();
            _notes.Clear();
            _tileRefs.Clear();
            foreach (var key in _keyTiles.Keys.ToList())
                SetTileActive(key, false);
            RefreshRows();
            UiHub.Notify("已清空录制内容");
        }

        #endregion

        #region 试听（音频，不发按键）

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (_previewPlayer.IsPlaying)
            {
                _previewPlayer.Stop();
                return;
            }

            if (_state == RecState.Recording) PauseRecording();
            StartPreviewPlayback();
        }

        private void StartPreviewPlayback()
        {
            var notes = BuildSongNotes();
            if (notes.Count == 0)
            {
                _ = UiHub.InfoAsync("提示", "还没有可试听的音符，先开始录制吧。");
                return;
            }

            RedrawTimeline();
            _previewPlayer.AudioEnabled = PreviewSoundCheckBox.IsChecked == true;
            if (!_previewPlayer.Play(notes)) return;

            _previewCursor = new Line
            {
                Stroke = PreviewCursorBrush,
                StrokeThickness = 2,
                Y1 = 0,
                Y2 = 58
            };
            Canvas.SetLeft(_previewCursor, 0);
            TimelineCanvas.Children.Add(_previewCursor);
            PreviewButton.Content = "■ 停止试听";
        }

        private void PreviewPlayer_NoteStarted(object? sender, Note note)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(note.Key)) return;
                RetainTile(note.Key);
                _previewTiles.Add(note.Key);
            });
        }

        private void PreviewPlayer_NoteEnded(object? sender, Note note)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(note.Key)) return;
                ReleaseTile(note.Key);
                int idx = _previewTiles.IndexOf(note.Key);
                if (idx >= 0) _previewTiles.RemoveAt(idx);
            });
        }

        private void PreviewPlayer_ProgressChanged(object? sender, double progress)
        {
            Dispatcher.Invoke(() =>
            {
                if (_previewCursor != null)
                {
                    Canvas.SetLeft(_previewCursor, progress * _displayTotalMs * TimelineScale);
                }
            });
        }

        private void EndPreviewUi()
        {
            if (_previewCursor != null)
            {
                TimelineCanvas.Children.Remove(_previewCursor);
                _previewCursor = null;
            }
            foreach (var key in _previewTiles)
                ReleaseTile(key);
            _previewTiles.Clear();
            PreviewButton.Content = "▶ 试听";
        }

        #endregion

        #region 保存

        private int MinDuration
        {
            get
            {
                if (int.TryParse(MinDurationBox.Text.Trim(), out int ms) && ms > 0)
                    return Math.Min(ms, 2000);
                return 50;
            }
        }

        private int Quantize(int value)
        {
            int step = _quantizeStep;
            if (step <= 0) return Math.Max(1, value);
            return Math.Max(1, (int)(Math.Round(value / (double)step) * step));
        }

        /// <summary>按录入时间顺序生成音符序列（含休止符，间隙自动补停顿）</summary>
        private List<Note> BuildSongNotes(double? nowMs = null)
        {
            var result = new List<Note>();
            int minDur = MinDuration;
            double cursor = 0;

            foreach (var n in _notes.OrderBy(x => x.StartMs))
            {
                int dur = n.Active && nowMs.HasValue
                    ? Math.Max(1, (int)(nowMs.Value - n.StartMs))
                    : n.Duration;
                if (dur <= 0) continue;

                double start = Math.Max(n.StartMs, cursor);
                if (start > cursor + GapThresholdMs)
                {
                    result.Add(new Note
                    {
                        Key = "",
                        Duration = Math.Max(1, Quantize((int)(start - cursor)))
                    });
                    cursor = start;
                }

                int finalDur = Math.Max(minDur, Quantize((int)(n.StartMs + dur - start)));
                result.Add(new Note
                {
                    Key = n.Key,
                    Duration = finalDur,
                    IsSharp = n.IsSharp,
                    IsFlat = n.IsFlat,
                    IsNatural = n.IsNatural
                });
                cursor = start + finalDur;
            }

            return result;
        }

        private async void SaveSong_Click(object sender, RoutedEventArgs e)
        {
            string songName = SongNameTextBox.Text.Trim();
            string artist = ArtistTextBox.Text.Trim();

            if (string.IsNullOrEmpty(songName))
            {
                await UiHub.InfoAsync("提示", "请先输入歌曲名称。");
                return;
            }
            if (string.IsNullOrEmpty(artist))
            {
                artist = "未知艺术家";
            }

            if (_previewPlayer.IsPlaying) _previewPlayer.Stop();
            FinalizeActiveNotes();

            var notes = BuildSongNotes();
            if (notes.Count == 0)
            {
                await UiHub.InfoAsync("提示", "还没有录制任何音符。点击「开始录制」并按下第一个按键即可开始。");
                return;
            }

            try
            {
                var song = new Song
                {
                    Name = songName,
                    Artist = artist,
                    Bpm = 120,
                    Notes = notes
                };

                string fileName = $"live_{Guid.NewGuid():N}.json";
                _songStorage.SaveSong(song, fileName);
                _songStorage.AddSongToList(new SongInfo
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = songName,
                    Artist = artist,
                    FileName = fileName
                });

                _shell.ReloadSongs();
                ResetForNextTake();
                UiHub.Notify($"已保存「{songName}」，可开始录制下一首");
            }
            catch (Exception ex)
            {
                await UiHub.InfoAsync("保存失败", ex.Message);
            }
        }

        private async void CancelSave_Click(object sender, RoutedEventArgs e)
        {
            bool ok = await UiHub.ConfirmAsync("取消保存", "确定丢弃本次录制的音符吗？此操作不可恢复。");
            if (!ok) return;

            ResetForNextTake();
            UiHub.Notify("已丢弃，可开始录制下一首");
        }

        /// <summary>保存/取消后重置：清空本次录制，回到待机，准备录下一首</summary>
        private void ResetForNextTake()
        {
            if (_previewPlayer.IsPlaying) _previewPlayer.Stop();
            _sound.StopAll();
            _monitorHandles.Clear();
            _activeNotes.Clear();
            _notes.Clear();
            _tileRefs.Clear();
            foreach (var key in _keyTiles.Keys.ToList())
                SetTileActive(key, false);

            _stopwatch.Reset();
            TimerText.Text = NoteRow.FormatMs(0);
            _state = RecState.Idle;
            UninstallHook();
            SongNameTextBox.Clear();   // 艺术家保留，方便连续录制同一人作品
            RefreshRows();
            UpdateStatus();
        }

        #endregion
    }
}
