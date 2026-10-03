using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GameMusicPlayer.Models;
using GameMusicPlayer.Services;

namespace GameMusicPlayer.Views
{
    public partial class RecordInputView : UserControl
    {
        private readonly MainWindow _shell;
        private readonly SongStorage _songStorage = new SongStorage();
        private readonly SongPreviewPlayer _previewPlayer;

        public RecordInputView(MainWindow shell)
        {
            InitializeComponent();
            _shell = shell;
            _previewPlayer = new SongPreviewPlayer(PreviewSoundService.Shared);
            Unloaded += (s, e) => _previewPlayer.Dispose();
        }

        /// <summary>设置简谱输入框的文本内容</summary>
        public void SetNotationText(string text)
        {
            NotationTextBox.Text = text;
        }

        private List<Note> ParseCurrent(out int ignored)
        {
            return _songStorage.ParseNotationDetailed(NotationTextBox.Text, GetBpm(), out ignored);
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (_previewPlayer.IsPlaying)
            {
                _previewPlayer.Stop();
                return;
            }

            var notes = ParseCurrent(out _);
            if (notes.Count == 0)
            {
                _ = UiHub.InfoAsync("提示", "还没有可试听的音符，请先输入简谱。");
                return;
            }

            _previewPlayer.AudioEnabled = true;
            if (_previewPlayer.Play(notes))
            {
                PreviewButton.Content = "■ 停止试听";
                _previewPlayer.PlaybackStopped += PreviewStopped;
                _previewPlayer.PlaybackFinished += PreviewStopped;
            }
        }

        private void PreviewStopped(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _previewPlayer.PlaybackStopped -= PreviewStopped;
                _previewPlayer.PlaybackFinished -= PreviewStopped;
                PreviewButton.Content = "▶ 试听";
            });
        }

        private void Script_Click(object sender, RoutedEventArgs e)
        {
            var notes = ParseCurrent(out int ignored);
            if (notes.Count == 0)
            {
                _ = UiHub.InfoAsync("提示", "还没有解析出音符，无法生成按键脚本。");
                return;
            }

            string name = SongNameTextBox.Text.Trim();
            var draft = new Song
            {
                Name = string.IsNullOrEmpty(name) ? "未保存草稿" : name,
                Artist = string.IsNullOrEmpty(ArtistTextBox.Text.Trim()) ? "未知艺术家" : ArtistTextBox.Text.Trim(),
                Bpm = GetBpm(),
                Notes = notes
            };
            UiHub.ShowScript?.Invoke($"按键脚本 · {draft.Name}", KeyScriptFormatter.Format(draft));
        }

        private void NotationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ParseStatusText == null) return;

            string text = NotationTextBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                ParseStatusText.Text = "等待输入…";
                ParseStatusText.Foreground = (System.Windows.Media.Brush)FindResource("SubTextBrush");
                return;
            }

            try
            {
                var notes = _songStorage.ParseNotationDetailed(text, GetBpm(), out int ignored);
                if (notes.Count == 0)
                {
                    ParseStatusText.Text = ignored > 0 ? $"未识别到音符（忽略 {ignored} 个标记）" : "未识别到音符";
                    ParseStatusText.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                }
                else
                {
                    ParseStatusText.Text = $"已识别 {notes.Count} 个事件 · 忽略 {ignored} 个标记";
                    ParseStatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                }
            }
            catch
            {
                ParseStatusText.Text = "解析出错";
                ParseStatusText.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
            }
        }

        private int GetBpm()
        {
            return int.TryParse(BpmTextBox.Text.Trim(), out int bpm) && bpm > 0 ? bpm : 120;
        }

        private async void SaveSong_Click(object sender, RoutedEventArgs e)
        {
            string songName = SongNameTextBox.Text.Trim();
            string artist = ArtistTextBox.Text.Trim();
            string notation = NotationTextBox.Text.Trim();

            if (string.IsNullOrEmpty(songName))
            {
                await UiHub.InfoAsync("提示", "请先输入歌曲名称。");
                return;
            }
            if (string.IsNullOrEmpty(notation))
            {
                await UiHub.InfoAsync("提示", "请先输入乐谱内容。");
                return;
            }
            if (string.IsNullOrEmpty(artist))
            {
                artist = "未知艺术家";
            }

            try
            {
                int bpm = GetBpm();
                List<Note> notes = _songStorage.ParseNotation(notation, bpm);
                if (notes.Count == 0)
                {
                    await UiHub.InfoAsync("提示", "未能解析出任何音符，请检查乐谱格式。");
                    return;
                }

                var song = new Song
                {
                    Name = songName,
                    Artist = artist,
                    Bpm = bpm,
                    Notes = notes
                };

                string fileName = $"{Guid.NewGuid():N}.json";
                _songStorage.SaveSong(song, fileName);
                _songStorage.AddSongToList(new SongInfo
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = songName,
                    Artist = artist,
                    FileName = fileName
                });

                _shell.ReloadSongs();
                UiHub.Notify($"已保存「{songName}」· {notes.Count} 个事件");

                SongNameTextBox.Clear();
                ArtistTextBox.Clear();
                NotationTextBox.Clear();
                _shell.Navigate("player");
            }
            catch (Exception ex)
            {
                await UiHub.InfoAsync("保存失败", ex.Message);
            }
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NotationTextBox.Text) && string.IsNullOrWhiteSpace(SongNameTextBox.Text))
                return;

            bool ok = await UiHub.ConfirmAsync("确认清空", "确定清空当前填写的内容吗？");
            if (!ok) return;

            SongNameTextBox.Clear();
            ArtistTextBox.Clear();
            NotationTextBox.Clear();
            BpmTextBox.Text = "120";
            UiHub.Notify("已清空");
        }
    }
}
