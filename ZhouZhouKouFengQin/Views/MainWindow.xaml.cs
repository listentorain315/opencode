using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GameMusicPlayer.Services;

namespace GameMusicPlayer.Views
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private readonly PlayerView _playerView;
        private readonly LiveRecordView _liveRecordView;
        private readonly RecordInputView _recordInputView;
        private readonly ImageRecognitionView _imageRecognitionView;
        private readonly HotKeyService _hotKeyService;

        private TaskCompletionSource<bool>? _overlayTcs;
        private bool _booted;
        private bool _switchingNav;

        public MainWindow()
        {
            InitializeComponent();

            _playerView = new PlayerView(this);
            _liveRecordView = new LiveRecordView(this);
            _recordInputView = new RecordInputView(this);
            _imageRecognitionView = new ImageRecognitionView(this);

            _hotKeyService = new HotKeyService(this);
            _hotKeyService.PreviousSongPressed += (s, e) => Dispatcher.Invoke(() =>
            {
                if (_playerView.IsPlaybackModeActive) _playerView.PreviousSong();
                else UiHub.Notify("请先点「开始播放」打开小助手");
            });
            _hotKeyService.PlayPausePressed += (s, e) => Dispatcher.Invoke(() =>
            {
                // 仅在播放模式（小助手已打开）下生效，防止其他界面误触发
                if (_playerView.IsPlaybackModeActive) _playerView.TogglePlayPause();
                else UiHub.Notify("请先点「开始播放」打开小助手");
            });
            _hotKeyService.NextSongPressed += (s, e) => Dispatcher.Invoke(() =>
            {
                if (_playerView.IsPlaybackModeActive) _playerView.NextSong();
                else UiHub.Notify("请先点「开始播放」打开小助手");
            });
            _hotKeyService.ToggleMiniPressed += (s, e) => Dispatcher.Invoke(() => _playerView.ToggleMiniPlayer());

            UiHub.ShowOverlay = ShowOverlayAsync;
            UiHub.PushToast = PushToast;
            UiHub.ShowScript = ShowScript;

            SourceInitialized += (s, e) => EnableRoundedCorners();
            StateChanged += (s, e) => MaxGlyph.Data = Geometry.Parse(WindowState == WindowState.Maximized
                ? "M1,3 H6 V8 H1 Z M3,3 V1 H8 V6"
                : "M1,1 H9 V9 H1 Z");
            Loaded += (s, e) => PreviewSoundService.Shared.WarmUp();

            GlobalVolumeSlider.Value = PreviewSoundService.Shared.Volume;

            _booted = true;
            Navigate("player");
        }

        #region 窗口控制（无边框）

        private void EnableRoundedCorners()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int preference = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(hwnd, 33, ref preference, sizeof(int));
            }
            catch
            {
                // Win10 及以下忽略
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }
            try { DragMove(); } catch { }
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region 导航

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (!_booted || _switchingNav) return;
            if (sender is RadioButton rb && rb.Tag is string key)
            {
                Navigate(key);
            }
        }

        public void Navigate(string key)
        {
            if (_switchingNav) return;
            _switchingNav = true;
            try
            {
                _playerView.StopAudioPreview();   // 切页 = 其他操作：中断试听

                UserControl view = key switch
                {
                    "live" => _liveRecordView,
                    "input" => _recordInputView,
                    "recognize" => _imageRecognitionView,
                    _ => _playerView
                };
                ContentHost.Content = view;

                NavPlayer.IsChecked = key == "player";
                NavLive.IsChecked = key == "live";
                NavInput.IsChecked = key == "input";
                NavRecognize.IsChecked = key == "recognize";

                if (key == "player") _playerView.Reload();
            }
            finally
            {
                _switchingNav = false;
            }
        }

        public void ReloadSongs() => _playerView.Reload();

        /// <summary>供其他视图调用：任何其他操作中断歌单试听</summary>
        public void StopPlayerPreview() => _playerView.StopAudioPreview();

        /// <summary>设置手动录入页面的文本内容</summary>
        public void SetRecordInputText(string text)
        {
            _recordInputView.SetNotationText(text);
        }

        public void SetGlobalStatus(string text, bool active = false)
        {
            GlobalStatusText.Text = text;
            StatusDot.Fill = active
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("MutedBrush");
        }

        #endregion

        #region 覆盖层 / Toast

        /// <summary>站内脚本查看器（替代弹窗）</summary>
        public void ShowScript(string title, string text)
        {
            ScriptTitle.Text = title;
            ScriptText.Text = text;
            ScriptOverlay.Visibility = Visibility.Visible;
            ScriptText.Focus();
            ScriptText.SelectAll();
        }

        private void ScriptClose_Click(object sender, RoutedEventArgs e)
        {
            ScriptOverlay.Visibility = Visibility.Collapsed;
        }

        private void ScriptCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ScriptText.Text);
                UiHub.Notify("按键脚本已复制到剪贴板");
            }
            catch (Exception ex)
            {
                UiHub.Notify("复制失败：" + ex.Message);
            }
        }

        public async Task<bool> ShowOverlayAsync(string title, string message, bool needConfirm)
        {
            OverlayTitle.Text = title;
            OverlayMessage.Text = message;
            OverlayCancel.Visibility = needConfirm ? Visibility.Visible : Visibility.Collapsed;
            OverlayConfirm.Content = needConfirm ? "确定" : "好的";
            OverlayLayer.Visibility = Visibility.Visible;

            _overlayTcs?.TrySetResult(false);
            _overlayTcs = new TaskCompletionSource<bool>();
            return await _overlayTcs.Task;
        }

        private void OverlayConfirm_Click(object sender, RoutedEventArgs e)
        {
            OverlayLayer.Visibility = Visibility.Collapsed;
            _overlayTcs?.TrySetResult(true);
        }

        private void OverlayCancel_Click(object sender, RoutedEventArgs e)
        {
            OverlayLayer.Visibility = Visibility.Collapsed;
            _overlayTcs?.TrySetResult(false);
        }

        private void PushToast(string message)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = (Brush)FindResource("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var text = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Foreground = (Brush)FindResource("TextBrush"),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(dot);
            panel.Children.Add(text);

            var card = new Border
            {
                Style = (Style)FindResource("ToastCard"),
                Padding = new Thickness(16, 11, 16, 11),
                Margin = new Thickness(0, 0, 0, 8),
                MaxWidth = 340,
                Child = panel
            };

            ToastHost.Items.Add(card);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.8) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                ToastHost.Items.Remove(card);
            };
            timer.Start();
        }

        #endregion

        #region 键盘路由（录制视图捕获按键）

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (ContentHost.Content == _liveRecordView && _liveRecordView.ProcessKeyDown(e))
            {
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (ContentHost.Content == _liveRecordView && _liveRecordView.ProcessKeyUp(e))
            {
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyUp(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            _liveRecordView.OnShellDeactivated();
            base.OnDeactivated(e);
        }

        #endregion

        private void GlobalVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_booted) PreviewSoundService.Shared.SetVolume(e.NewValue);
        }

        protected override void OnClosed(EventArgs e)
        {
            _playerView.Shutdown();
            _liveRecordView.Shutdown();
            _hotKeyService.Dispose();
            PreviewSoundService.Shared.Dispose();
            base.OnClosed(e);
        }
    }
}
