using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GameMusicPlayer.Models;
using GameMusicPlayer.Services;

namespace GameMusicPlayer.Views
{
    public partial class PlayerView : UserControl
    {
        private readonly MainWindow _shell;
        private readonly SongStorage _songStorage;
        private readonly PlaybackEngine _playbackEngine;
        private readonly PreviewSoundService _soundService;
        private readonly SongPreviewPlayer _previewPlayer;
        private readonly ObservableCollection<Song> _songs = new ObservableCollection<Song>();
        private MiniPlayerWindow? _miniWindow;
        private Song? _previewSong;

        /// <summary>游戏按键播放引擎（小助手/全局热键共用同一实例）</summary>
        public PlaybackEngine Engine => _playbackEngine;

        /// <summary>歌单数据（小助手歌单列表共用）</summary>
        public ObservableCollection<Song> Songs => _songs;

        /// <summary>播放模式 = 小助手已打开；F8/F9 仅在此模式下生效</summary>
        public bool IsPlaybackModeActive => _miniWindow?.IsVisible == true;

        public PlayerView(MainWindow shell)
        {
            InitializeComponent();

            _shell = shell;
            _songStorage = new SongStorage();
            _playbackEngine = new PlaybackEngine();
            _soundService = PreviewSoundService.Shared;
            _previewPlayer = new SongPreviewPlayer(_soundService);

            SongListBox.ItemsSource = _songs;

            _playbackEngine.PlaybackStarted += OnPlaybackStarted;
            _playbackEngine.PlaybackStopped += OnPlaybackStopped;
            _playbackEngine.PlaybackPaused += OnPlaybackPaused;
            _playbackEngine.PlaybackResumed += OnPlaybackResumed;
            _playbackEngine.ProgressChanged += OnProgressChanged;

            _previewPlayer.ProgressChanged += OnPreviewProgress;
            _previewPlayer.PlaybackFinished += (s, e) => Dispatcher.Invoke(ResetPreviewUi);
            _previewPlayer.PlaybackStopped += (s, e) => Dispatcher.Invoke(ResetPreviewUi);

            Reload();
        }

        public void Shutdown()
        {
            try { _miniWindow?.Close(); } catch { }
            _previewPlayer.Dispose();
            _playbackEngine.Dispose();
        }

        #region 歌单

        public void Reload()
        {
            _songs.Clear();
            var songList = _songStorage.GetSongList();
            foreach (var songInfo in songList.Songs)
            {
                var song = _songStorage.LoadSong(songInfo.FileName);
                if (song != null)
                {
                    song.Id = songInfo.Id;
                    _songs.Add(song);
                }
            }
            SongCountText.Text = $" {_songs.Count} 首";
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string songId) return;

            bool ok = await UiHub.ConfirmAsync("确认删除", "确定要删除这首歌曲吗？删除后不可恢复。");
            if (!ok) return;

            StopAudioPreview();
            _songStorage.RemoveSongFromList(songId);
            Reload();
            UiHub.Notify("歌曲已删除");
        }

        private void Script_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string songId) return;

            StopAudioPreview();

            var song = LoadById(songId);
            if (song == null) return;
            UiHub.ShowScript?.Invoke($"按键脚本 · {song.Name}", KeyScriptFormatter.Format(song));
        }

        private Song? LoadById(string songId)
        {
            var songList = _songStorage.GetSongList();
            var songInfo = songList.Songs.Find(s => s.Id == songId);
            return songInfo == null ? null : _songStorage.LoadSong(songInfo.FileName);
        }

        private void SongListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_previewSong != null && SongListBox.SelectedItem is Song sel && sel.Id != _previewSong.Id)
            {
                StopAudioPreview();
            }

            if (SongListBox.SelectedItem is Song song)
            {
                CurrentSongName.Text = song.Name;
                CurrentSongArtist.Text = song.Artist;
            }
        }

        private void SongListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 主界面不直接发键：双击仅打开小助手，在小助手里操作播放
            ShowMiniPlayer();
        }

        #endregion

        #region 小助手 / 游戏播放（发送按键）

        /// <summary>打开小助手（播放模式入口；真正的脚本播放只在小助手内操作）</summary>
        private void StartPlay_Click(object sender, RoutedEventArgs e) => ShowMiniPlayer();

        public void ShowMiniPlayer()
        {
            _miniWindow ??= new MiniPlayerWindow(this);
            _miniWindow.ShowPlayer();
        }

        public void ToggleMiniPlayer()
        {
            _miniWindow ??= new MiniPlayerWindow(this);
            if (_miniWindow.IsVisible) _miniWindow.HidePlayer();
            else _miniWindow.ShowPlayer();
        }

        /// <summary>选中歌曲并载入引擎（不发键）</summary>
        public void SelectSong(string songId)
        {
            var songList = _songStorage.GetSongList();
            var songInfo = songList.Songs.Find(s => s.Id == songId);
            if (songInfo == null) return;
            StopAudioPreview();
            _playbackEngine.LoadSong(songInfo.FileName);
        }

        /// <summary>播放指定歌曲（发键给游戏；仅小助手调用）</summary>
        public void PlaySongById(string songId)
        {
            var songList = _songStorage.GetSongList();
            var songInfo = songList.Songs.Find(s => s.Id == songId);
            if (songInfo == null) return;
            StopAudioPreview();
            _playbackEngine.PlaySong(songInfo.FileName);
        }

        public void TogglePlayPause()
        {
            if (!_playbackEngine.IsPlaying)
            {
                if (_playbackEngine.CurrentSong == null && _songs.Count > 0)
                {
                    var songList = _songStorage.GetSongList();
                    if (songList.Songs.Count > 0)
                        _playbackEngine.LoadSong(songList.Songs[0].FileName);
                }
                StopAudioPreview();
                _playbackEngine.Play();
            }
            else
            {
                _playbackEngine.TogglePlayPause();
            }
        }

        public void NextSong()
        {
            StopAudioPreview();
            _playbackEngine.PlayNext();
        }

        public void PreviousSong()
        {
            StopAudioPreview();
            _playbackEngine.PlayPrevious();
        }

        /// <summary>脚本播放速度（小助手变速按钮）</summary>
        public void SetSpeed(double multiplier) => _playbackEngine.BpmMultiplier = multiplier;

        public double GetSpeed() => _playbackEngine.BpmMultiplier;

        private void OnPlaybackStarted(object? sender, Song song)
        {
            Dispatcher.Invoke(() =>
            {
                NowPlayingText.Text = "脚本播放中 · 发送按键到游戏";
                CurrentSongName.Text = song.Name;
                CurrentSongArtist.Text = song.Artist;
                _shell.SetGlobalStatus($"正在播放: {song.Name}", true);
            });
        }

        private void OnPlaybackPaused(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                NowPlayingText.Text = "已暂停";
                _shell.SetGlobalStatus("播放已暂停");
            });
        }

        private void OnPlaybackResumed(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                NowPlayingText.Text = "脚本播放中 · 发送按键到游戏";
                _shell.SetGlobalStatus("正在播放", true);
            });
        }

        private void OnPlaybackStopped(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                NowPlayingText.Text = "未在播放";
                PlaybackProgress.Value = 0;
                _shell.SetGlobalStatus("就绪");
            });
        }

        private void OnProgressChanged(object? sender, double progress)
        {
            Dispatcher.Invoke(() => PlaybackProgress.Value = progress * 100);
        }

        #endregion

        #region 音频试听（只播采样，不发按键）

        private void PreviewSong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string songId) return;

            if (_previewPlayer.IsPlaying && _previewSong?.Id == songId)
            {
                StopAudioPreview();
                return;
            }

            var song = LoadById(songId);
            if (song != null) StartAudioPreview(song);
        }

        private void PreviewAudio_Click(object sender, RoutedEventArgs e)
        {
            if (_previewPlayer.IsPlaying)
            {
                StopAudioPreview();
                return;
            }

            Song? song = null;
            if (SongListBox.SelectedItem is Song selected)
            {
                song = LoadById(selected.Id);
            }
            else if (_songs.Count > 0)
            {
                var songList = _songStorage.GetSongList();
                if (songList.Songs.Count > 0)
                    song = _songStorage.LoadSong(songList.Songs[0].FileName);
            }

            if (song == null)
            {
                _ = UiHub.InfoAsync("提示", "请先选择一首歌曲。");
                return;
            }

            StartAudioPreview(song);
        }

        private void StartAudioPreview(Song song)
        {
            if (_previewPlayer.IsPlaying) _previewPlayer.Stop();

            _previewSong = song;
            _previewPlayer.AudioEnabled = true;
            if (!_previewPlayer.Play(song.Notes))
            {
                _previewSong = null;
                _ = UiHub.InfoAsync("提示", "这首歌没有可试听的音符。");
                return;
            }

            NowPlayingText.Text = "音频试听中 · 不发送按键";
            CurrentSongName.Text = song.Name;
            CurrentSongArtist.Text = song.Artist;
            PreviewAudioButton.Content = "■ 停止试听";
            _shell.SetGlobalStatus($"音频试听: {song.Name}", true);
        }

        /// <summary>停止歌单/音频试听（任何其他操作都应调用）</summary>
        public void StopAudioPreview()
        {
            if (_previewPlayer.IsPlaying)
            {
                _previewPlayer.Stop();
            }
            else if (_previewSong != null)
            {
                ResetPreviewUi();
            }
        }

        private void ResetPreviewUi()
        {
            _previewSong = null;
            PlaybackProgress.Value = 0;
            PreviewAudioButton.Content = "♪ 试听音频（不发按键）";
            if (!_playbackEngine.IsPlaying)
            {
                NowPlayingText.Text = "未在播放";
                _shell.SetGlobalStatus("就绪");
            }
        }

        private void OnPreviewProgress(object? sender, double progress)
        {
            Dispatcher.Invoke(() => PlaybackProgress.Value = progress * 100);
        }

        #endregion
    }
}
