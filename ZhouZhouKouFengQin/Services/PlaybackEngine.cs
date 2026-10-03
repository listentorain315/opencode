using System;
using System.Threading;
using System.Threading.Tasks;
using GameMusicPlayer.Models;

namespace GameMusicPlayer.Services
{
    public class PlaybackEngine : IDisposable
    {
        private readonly KeySimulator _keySimulator;
        private readonly SongStorage _songStorage;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _playbackTask;
        private bool _isPlaying;
        private volatile bool _isPaused;
        private Song? _currentSong;
        private string? _currentFileName;
        private int _currentNoteIndex;
        private double _bpmMultiplier = 1.0;

        public event EventHandler<Song>? PlaybackStarted;
        public event EventHandler? PlaybackStopped;
        public event EventHandler? PlaybackPaused;
        public event EventHandler? PlaybackResumed;
        public event EventHandler<int>? NotePlayed;
        public event EventHandler<double>? ProgressChanged;

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public Song? CurrentSong => _currentSong;

        public double BpmMultiplier
        {
            get => _bpmMultiplier;
            set => _bpmMultiplier = Math.Max(0.1, Math.Min(3.0, value));
        }

        public PlaybackEngine()
        {
            _keySimulator = new KeySimulator();
            _songStorage = new SongStorage();
        }

        /// <summary>
        /// 加载歌曲
        /// </summary>
        public bool LoadSong(string fileName)
        {
            _currentSong = _songStorage.LoadSong(fileName);
            _currentFileName = _currentSong != null ? fileName : null;
            _currentNoteIndex = 0;
            return _currentSong != null;
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        public void Play()
        {
            if (_currentSong == null || _isPlaying)
                return;

            // 播完后再次播放 = 从头重新播
            if (_currentNoteIndex >= _currentSong.Notes.Count)
                _currentNoteIndex = 0;

            _isPlaying = true;
            _isPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();

            PlaybackStarted?.Invoke(this, _currentSong);

            _playbackTask = Task.Run(() => PlaybackLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (_isPlaying && !_isPaused)
            {
                _isPaused = true;
                PlaybackPaused?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 继续播放
        /// </summary>
        public void Resume()
        {
            if (_isPlaying && _isPaused)
            {
                _isPaused = false;
                PlaybackResumed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 切换播放/暂停状态
        /// </summary>
        public void TogglePlayPause()
        {
            if (!_isPlaying)
            {
                Play();
            }
            else if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            if (!_isPlaying)
                return;

            _cancellationTokenSource?.Cancel();
            _isPlaying = false;
            _isPaused = false;

            try
            {
                // 按键持有采用分片睡眠，取消后 ~25ms 内退出，UI 不会被长阻塞
                _playbackTask?.Wait(TimeSpan.FromMilliseconds(400));
            }
            catch (AggregateException)
            {
                // 忽略任务取消异常
            }

            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 播放下一首
        /// </summary>
        public void PlayNext()
        {
            var songList = _songStorage.GetSongList();
            if (songList.Songs.Count == 0)
                return;

            Stop();

            int currentIndex = FindCurrentIndex(songList);
            int nextIndex = (currentIndex + 1) % songList.Songs.Count;
            var nextSong = songList.Songs[nextIndex];

            if (LoadSong(nextSong.FileName))
            {
                Play();
            }
        }

        /// <summary>
        /// 播放上一首
        /// </summary>
        public void PlayPrevious()
        {
            var songList = _songStorage.GetSongList();
            if (songList.Songs.Count == 0)
                return;

            Stop();

            int currentIndex = FindCurrentIndex(songList);
            int prevIndex = currentIndex <= 0 ? songList.Songs.Count - 1 : currentIndex - 1;
            var prevSong = songList.Songs[prevIndex];

            if (LoadSong(prevSong.FileName))
            {
                Play();
            }
        }

        private int FindCurrentIndex(SongList songList)
        {
            if (_currentFileName != null)
            {
                int index = songList.Songs.FindIndex(s => s.FileName == _currentFileName);
                if (index >= 0)
                    return index;
            }
            if (_currentSong != null)
            {
                return songList.Songs.FindIndex(s => s.Name == _currentSong.Name);
            }
            return -1;
        }

        /// <summary>
        /// 播放指定歌曲
        /// </summary>
        public void PlaySong(string fileName)
        {
            Stop();
            if (LoadSong(fileName))
            {
                Play();
            }
        }

        private void PlaybackLoop(CancellationToken cancellationToken)
        {
            if (_currentSong == null)
                return;

            // 按下即播放：无倒计时
            if (cancellationToken.IsCancellationRequested)
            {
                _isPlaying = false;
                _isPaused = false;
                return;
            }

            var notes = _currentSong.Notes;
            int totalNotes = notes.Count;

            for (int i = _currentNoteIndex; i < totalNotes; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // 等待暂停状态结束
                while (_isPaused && !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;

                var note = notes[i];

                // 计算实际持续时间（考虑BPM倍率）
                int actualDuration = (int)(note.Duration / _bpmMultiplier);
                if (actualDuration < 10) actualDuration = 10;

                // 播放音符（暂停/停止 25ms 内即时打断按键持有）
                _keySimulator.PlayNote(note, actualDuration,
                    () => !_isPaused && !cancellationToken.IsCancellationRequested);

                _currentNoteIndex = i + 1;

                // 更新进度
                double progress = (double)(i + 1) / totalNotes;
                ProgressChanged?.Invoke(this, progress);
                NotePlayed?.Invoke(this, i);
            }

            _isPlaying = false;
            _isPaused = false;
            if (!cancellationToken.IsCancellationRequested)
            {
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}