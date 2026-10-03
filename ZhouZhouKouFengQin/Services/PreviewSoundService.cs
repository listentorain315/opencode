using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameMusicPlayer.Services
{
    /// <summary>
    /// 钢琴采样音频服务：按键/音符 → 音高采样，多路并行播放。
    /// 每个音高持有 VoicesPerNote 路独立音频流（轮转使用）：
    ///   - 同音连奏 / 不同按键命中同一音高时互不打断
    ///   - 预热后尽量不 Open/Close 音源；同路重触发才截断旧音
    ///   - 尾音限时淡出（DefaultTailMs），池满抢占发声最久的一路
    /// 键位语义：左键=降八度 · 右键=升八度 · 中键=半音辅助（单独无效）
    ///           中键+左=降半音 · 中键+右=升半音
    /// </summary>
    public sealed class PreviewSoundService : IDisposable
    {
        /// <summary>音符结束 / 松键后的尾音长度（毫秒）</summary>
        public const double DefaultTailMs = 450;

        /// <summary>每个音高并行播放的音频流数量（同音/撞音不互相打断）</summary>
        public const int VoicesPerNote = 3;

        private const double FadeMs = 150;
        private const double AbsoluteCapMs = 5000;

        private static readonly Lazy<PreviewSoundService> LazyShared = new(() => new PreviewSoundService());

        /// <summary>全局共享实例（播放台 / 录制共用同一音色与音量）</summary>
        public static PreviewSoundService Shared => LazyShared.Value;

        private static readonly Dictionary<string, int> BaseSemitone = new(StringComparer.OrdinalIgnoreCase)
        {
            { "z", 0 }, { "x", 2 }, { "c", 4 }, { "v", 5 }, { "b", 7 }, { "n", 9 }, { "m", 11 }, { ",", 12 },
            { "1", 0 }, { "2", 2 }, { "3", 4 }, { "4", 5 }, { "5", 7 }, { "6", 9 }, { "7", 11 },
        };

        private static readonly string[] PitchNames =
            { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        private sealed class Voice
        {
            public MediaPlayer Player = null!;
            public int Midi;
            public bool Opened;
            public bool OpenPending;
            public double OpenRequestedMs;
            public bool PendingPlay;
            public bool Busy;
            public DateTime StartedUtc;
            public double StopAtMs;
        }

        private readonly Dictionary<int, List<Voice>> _pools = new();
        private readonly Dictionary<int, string> _files = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly DispatcherTimer _tailTimer;
        private bool _disposed;

        public double Volume { get; private set; } = 0.7;
        public int FileCount => _files.Count;
        public int OpenedCount { get; private set; }
        public int FailedCount { get; private set; }
        public int BusyVoiceCount
        {
            get
            {
                int c = 0;
                foreach (var pool in _pools.Values)
                    foreach (var v in pool)
                        if (v.Busy) c++;
                return c;
            }
        }
        public bool IsWarmedUp { get; private set; }

        private PreviewSoundService()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", "piano");
            for (int midi = 36; midi <= 96; midi++) // C2 ~ C7 全音域（±12 移调全覆盖）
            {
                string name = FileNameForMidi(midi);
                string wav = Path.Combine(dir, name + ".wav");
                string mp3 = Path.Combine(dir, name + ".mp3");
                if (File.Exists(wav)) _files[midi] = wav;
                else if (File.Exists(mp3)) _files[midi] = mp3;
            }

            _tailTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
            _tailTimer.Tick += TailTimer_Tick;
            _tailTimer.Start();
        }

        /// <summary>
        /// 预热：为每个音高常驻打开 VoicesPerNote 路音频流（异步完成）。
        /// 之后发声尽量不 Close/Open，直接从头播放。
        /// </summary>
        public void WarmUp()
        {
            if (_disposed || IsWarmedUp) return;
            IsWarmedUp = true;
            foreach (int midi in _files.Keys.OrderBy(k => k))
            {
                foreach (var voice in GetPool(midi))
                    OpenSource(voice, midi);
            }
        }

        private List<Voice> GetPool(int midi)
        {
            if (_pools.TryGetValue(midi, out var pool)) return pool;

            pool = new List<Voice>(VoicesPerNote);
            for (int i = 0; i < VoicesPerNote; i++)
            {
                var voice = new Voice { Midi = midi };
                var mp = new MediaPlayer { Volume = Volume };
                var captured = voice;
                mp.MediaOpened += (s, e) =>
                {
                    captured.Opened = true;
                    captured.OpenPending = false;
                    OpenedCount++;
                    // 冷流就绪即播：保证按键最迟在打开完成后发声（杜绝永久哑音）
                    if (captured.PendingPlay)
                    {
                        captured.PendingPlay = false;
                        // 仅在发声时窗内起播，避免打开迟到补发"幽灵音"
                        if (_clock.Elapsed.TotalMilliseconds <= captured.StopAtMs)
                        {
                            try { captured.Player.Volume = Volume; captured.Player.Play(); } catch { }
                        }
                    }
                };
                mp.MediaFailed += (s, e) => { captured.Opened = false; captured.OpenPending = false; captured.PendingPlay = false; FailedCount++; };
                voice.Player = mp;
                pool.Add(voice);
            }
            _pools[midi] = pool;
            return pool;
        }

        private void OpenSource(Voice voice, int midi)
        {
            try
            {
                voice.Player.Stop();
                voice.Player.Close();
                voice.Player.Open(new Uri(_files[midi]));
                voice.Opened = false;
                voice.OpenPending = true;
                voice.OpenRequestedMs = _clock.Elapsed.TotalMilliseconds;
            }
            catch
            {
                FailedCount++;
                voice.OpenPending = false;
            }
        }

        /// <summary>MIDI 音号 → 采样文件名（降号命名，如 61 → Db4）</summary>
        public static string FileNameForMidi(int midi)
        {
            int pc = ((midi % 12) + 12) % 12;
            int octave = midi / 12 - 1;
            return $"{PitchNames[pc]}{octave}";
        }

        /// <summary>
        /// 按键 + 鼠标修饰 → MIDI 音号。
        /// 左键(flat)=降八度 · 右键(sharp)=升八度 · 中键(natural)=半音辅助（单独无效）
        /// 中键+左=降半音 · 中键+右=升半音。1-7 与 z-m 同音高。
        /// </summary>
        public static bool TryGetMidi(string key, bool sharp, bool flat, bool natural, out int midi)
        {
            midi = 0;
            if (string.IsNullOrEmpty(key)) return false;
            if (!BaseSemitone.TryGetValue(key, out int semi)) return false;

            int offset;
            if (natural && sharp) offset = 1;        // 中+右 = 升半音
            else if (natural && flat) offset = -1;   // 中+左 = 降半音
            else if (sharp && !flat) offset = 12;    // 右   = 升八度
            else if (flat && !sharp) offset = -12;   // 左   = 降八度
            else offset = 0;                          // 中单独 / 无 = 本音

            midi = 60 + semi + offset;
            return true;
        }

        /// <summary>播放按键对应采样，返回句柄（用于延迟停止）；失败返回 null</summary>
        public object? PlayKey(string key, bool sharp, bool flat, bool natural, double ringMs = AbsoluteCapMs)
        {
            if (!TryGetMidi(key, sharp, flat, natural, out int midi)) return null;
            return PlayMidi(midi, ringMs);
        }

        /// <summary>
        /// 播放一个采样。该音高的多路音频流轮转使用：
        /// 同音连奏 / 撞音各占一路互不打断；仅池满时抢占最旧一路。
        /// ringMs 后自动淡出停止。
        /// </summary>
        public object? PlayMidi(int midi, double ringMs = AbsoluteCapMs)
        {
            if (_disposed) return null;
            if (!_files.ContainsKey(midi)) return null;

            var voice = Rent(midi);
            double now = _clock.Elapsed.TotalMilliseconds;
            try
            {
                if (voice.Opened)
                {
                    // 热路径：已打开的流，直接从头播放（不 Close/Open）
                    try { voice.Player.Stop(); } catch { }
                    try { voice.Player.Position = TimeSpan.Zero; } catch { }
                }
                else if (!voice.OpenPending || now - voice.OpenRequestedMs > 1500)
                {
                    // 未预热到 / 打开事件丢失超时：重新打开一次，Play 会在就绪后自动起播
                    OpenSource(voice, midi);
                }
                voice.Player.Volume = Volume;
                if (!voice.Opened) voice.PendingPlay = true;   // 打开完成后自动起播
                voice.Player.Play();
                voice.Busy = true;
                voice.StartedUtc = DateTime.UtcNow;
                voice.StopAtMs = now + Math.Max(120, ringMs);
                return voice;
            }
            catch
            {
                FailedCount++;
                voice.Busy = false;
                return null;
            }
        }

        /// <summary>在该音高的流池里租一路：优先空闲（最久未用），池满抢占发声最久的</summary>
        private Voice Rent(int midi)
        {
            var pool = GetPool(midi);

            Voice? free = null;
            foreach (var v in pool)
            {
                if (!v.Busy && (free == null || v.StartedUtc < free.StartedUtc))
                    free = v;
            }
            if (free != null) return free;

            Voice oldest = pool[0];
            foreach (var v in pool)
                if (v.StartedUtc < oldest.StartedUtc) oldest = v;
            return oldest;
        }

        /// <summary>延迟 delayMs 后淡出并停止该音（用于松键/音符结束收尾）</summary>
        public void ScheduleStop(object? handle, double delayMs)
        {
            if (handle is not Voice voice) return;
            voice.StopAtMs = _clock.Elapsed.TotalMilliseconds + Math.Max(60, delayMs);
        }

        /// <summary>立即停止该音</summary>
        public void StopVoice(object? handle)
        {
            if (handle is Voice voice) StopVoiceInternal(voice);
        }

        private void StopVoiceInternal(Voice voice)
        {
            try { voice.Player.Stop(); } catch { }
            voice.PendingPlay = false;
            voice.Busy = false;
        }

        private void TailTimer_Tick(object? sender, EventArgs e)
        {
            double now = _clock.Elapsed.TotalMilliseconds;
            foreach (var pool in _pools.Values)
            {
                foreach (var v in pool)
                {
                    if (!v.Busy) continue;
                    double remain = v.StopAtMs - now;
                    if (remain <= 0)
                    {
                        StopVoiceInternal(v);
                    }
                    else if (remain < FadeMs)
                    {
                        try { v.Player.Volume = Volume * (remain / FadeMs); } catch { }
                    }
                }
            }
        }

        public void SetVolume(double volume)
        {
            Volume = Math.Max(0, Math.Min(1, volume));
            foreach (var pool in _pools.Values)
            {
                foreach (var v in pool)
                {
                    try { v.Player.Volume = Volume; } catch { }
                }
            }
        }

        /// <summary>立即停止所有声音（手动停止/清空/关窗用）</summary>
        public void StopAll()
        {
            foreach (var pool in _pools.Values)
                foreach (var v in pool)
                    StopVoiceInternal(v);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _tailTimer.Stop();
            foreach (var pool in _pools.Values)
            {
                foreach (var v in pool)
                {
                    try { v.Player.Stop(); v.Player.Close(); } catch { }
                    v.Busy = false;
                }
            }
        }
    }
}
