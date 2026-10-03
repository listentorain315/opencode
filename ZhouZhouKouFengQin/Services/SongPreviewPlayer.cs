using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using GameMusicPlayer.Models;

namespace GameMusicPlayer.Services
{
    /// <summary>
    /// 歌曲音频试听调度器：按音符时间表并行触发采样发声。
    /// 只播放对应按键的音频，绝不发送按键脚本（SendInput）。
    /// 时间轴到总长即结束调度（不再触发新音）；每个音尾音限时（见 PreviewSoundService.DefaultTailMs）；
    /// 手动 Stop 立即全静音。
    /// </summary>
    public sealed class SongPreviewPlayer : IDisposable
    {
        private sealed class Scheduled
        {
            public double StartMs;
            public double EndMs;
            public int Midi;
            public Note Note = null!;
        }

        private readonly PreviewSoundService _sound;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly List<Scheduled> _schedule = new List<Scheduled>();
        private readonly List<Scheduled> _ringing = new List<Scheduled>();
        private double _totalMs;
        private int _nextIndex;

        public bool AudioEnabled { get; set; } = true;
        public bool IsPlaying { get; private set; }
        public double TotalMs => _totalMs;

        public event EventHandler<double>? ProgressChanged;
        public event EventHandler<Note>? NoteStarted;
        public event EventHandler<Note>? NoteEnded;
        public event EventHandler? PlaybackFinished;
        public event EventHandler? PlaybackStopped;

        public SongPreviewPlayer(PreviewSoundService sound)
        {
            _sound = sound;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            _timer.Tick += OnTick;
        }

        public bool Play(IReadOnlyList<Note>? notes)
        {
            Stop();
            _sound.StopAll();

            _schedule.Clear();
            double cursor = 0;
            if (notes != null)
            {
                foreach (var n in notes)
                {
                    int dur = Math.Max(1, n.Duration);
                    double start = cursor;
                    cursor += dur;
                    if (string.IsNullOrEmpty(n.Key)) continue;
                    if (!PreviewSoundService.TryGetMidi(n.Key, n.IsSharp, n.IsFlat, n.IsNatural, out int midi)) continue;
                    _schedule.Add(new Scheduled { StartMs = start, EndMs = cursor, Midi = midi, Note = n });
                }
            }
            if (_schedule.Count == 0) return false;

            _totalMs = cursor;
            _nextIndex = 0;
            _ringing.Clear();
            _clock.Restart();
            IsPlaying = true;
            _timer.Start();
            OnTick(this, EventArgs.Empty);
            return true;
        }

        /// <summary>手动停止：立即全静音（含延音）</summary>
        public void Stop()
        {
            if (!IsPlaying) return;
            _timer.Stop();
            IsPlaying = false;
            _sound.StopAll();
            _ringing.Clear();
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            double now = _clock.Elapsed.TotalMilliseconds;

            while (_nextIndex < _schedule.Count && _schedule[_nextIndex].StartMs <= now)
            {
                var s = _schedule[_nextIndex++];
                if (AudioEnabled)
                {
                    // 发声到音符结束 + 短尾音，避免尾音堆积
                    double ringMs = Math.Max(120, s.EndMs - now + PreviewSoundService.DefaultTailMs);
                    _sound.PlayMidi(s.Midi, ringMs);
                }
                _ringing.Add(s);
                NoteStarted?.Invoke(this, s.Note);
            }

            for (int i = _ringing.Count - 1; i >= 0; i--)
            {
                if (now >= _ringing[i].EndMs)
                {
                    NoteEnded?.Invoke(this, _ringing[i].Note);
                    _ringing.RemoveAt(i);
                }
            }

            if (now >= _totalMs)
            {
                _timer.Stop();
                IsPlaying = false;
                foreach (var s in _ringing) NoteEnded?.Invoke(this, s.Note);
                _ringing.Clear();
                ProgressChanged?.Invoke(this, 1.0);
                // 自然结束：不 StopAll，让最后的钢琴延音自然衰减完
                PlaybackFinished?.Invoke(this, EventArgs.Empty);
                return;
            }

            ProgressChanged?.Invoke(this, _totalMs > 0 ? now / _totalMs : 0);
        }

        public void Dispose()
        {
            Stop();
            _timer.Stop();
        }
    }
}
