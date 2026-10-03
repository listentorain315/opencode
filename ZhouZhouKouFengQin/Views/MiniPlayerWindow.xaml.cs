using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using GameMusicPlayer.Models;
using GameMusicPlayer.Services;

namespace GameMusicPlayer.Views
{
    /// <summary>
    /// 悬浮小助手：透明无边框、置顶显示在游戏画面上，不切屏、不抢游戏焦点（WS_EX_NOACTIVATE）。
    /// 只有在这里操作才会真正向游戏发送按键脚本；支持歌单选择、脚本变速、` 键显隐。
    /// 不会最小化（关闭仅由 ✕ 手动隐藏）。
    /// </summary>
    public partial class MiniPlayerWindow : Window
    {
        private const int GwlExstyle = -20;
        private const int WsExNoactivate = 0x08000000;

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private readonly PlayerView _player;
        private bool _positioned;

        public MiniPlayerWindow(PlayerView player)
        {
            InitializeComponent();
            _player = player;

            var engine = player.Engine;
            engine.PlaybackStarted += (s, song) => Dispatcher.Invoke(() =>
            {
                SongText.Text = $"{song.Name} — {song.Artist}";
                PlayButton.Content = "⏸ F8";
                StatusText.Text = "洲洲口风琴家 · 小助手";
                ScrollToPlayingSong(song.Name);
            });
            engine.PlaybackPaused += (s, e) => Dispatcher.Invoke(() =>
            {
                PlayButton.Content = "▶ F8";
                StatusText.Text = "已暂停";
            });
            engine.PlaybackResumed += (s, e) => Dispatcher.Invoke(() =>
            {
                PlayButton.Content = "⏸ F8";
                StatusText.Text = "正在播放";
            });
            engine.PlaybackStopped += (s, e) => Dispatcher.Invoke(() =>
            {
                PlayButton.Content = "▶ F8";
                StatusText.Text = "已结束";
            });
            engine.ProgressChanged += (s, p) => { };

            MiniSongList.ItemsSource = player.Songs;

            // 不激活窗口：点按钮不抢游戏焦点，键盘输入继续给游戏
            SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int style = GetWindowLong(hwnd, GwlExstyle);
                SetWindowLong32(hwnd, GwlExstyle, style | WsExNoactivate);
            };

            // 不可最小化：任何最小化请求都弹回正常状态（✕ 才是关闭）
            StateChanged += (s, e) =>
            {
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            };

            Loaded += (s, e) => PlaceBottomRight();
        }

        private void PlaceBottomRight()
        {
            if (_positioned) return;
            _positioned = true;
            var area = SystemParameters.WorkArea;
            Left = Math.Max(area.Left, area.Right - ActualWidth - 24);
            Top = Math.Max(area.Top, area.Bottom - ActualHeight - 24);
        }

        public void ShowPlayer()
        {
            Show();
            WindowState = WindowState.Normal;
            Topmost = true;
            if (MiniSongList.SelectedItem is Song sel)
                SongText.Text = $"{sel.Name} — {sel.Artist}";
        }

        public void HidePlayer() => Hide();

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try { DragMove(); } catch { }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) => _player.TogglePlayPause();

        private void PrevButton_Click(object sender, RoutedEventArgs e) => _player.PreviousSong();

        private void NextButton_Click(object sender, RoutedEventArgs e) => _player.NextSong();

        private void MiniSongList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MiniSongList.SelectedItem is Song song)
            {
                SongText.Text = $"{song.Name} — {song.Artist}";
                StatusText.Text = "已选中 · 双击或 ▶ 播放";
                _player.SelectSong(song.Id);   // 载入引擎但不发键
            }
        }

        private void MiniSongList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 双击可靠切换：在按下事件里直接取行并播放（避免双击丢失/事件竞争）
            if (e.ClickCount != 2) return;
            if (FindSongFromSource(e.OriginalSource as DependencyObject) is Song song)
            {
                _player.PlaySongById(song.Id);
                e.Handled = true;
            }
        }

        private void MiniSongList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MiniSongList.SelectedItem is Song song)
            {
                _player.PlaySongById(song.Id);
            }
        }

        private static Song? FindSongFromSource(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is System.Windows.Controls.ListBoxItem item && item.Content is Song song)
                    return song;
                current = current is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                    ? System.Windows.Media.VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>播放的歌曲若不在可视区则自动滚动到它；已可见则不动</summary>
        private void ScrollToPlayingSong(string songName)
        {
            Song? match = null;
            foreach (var s in _player.Songs)
            {
                if (s.Name == songName) { match = s; break; }
            }
            if (match == null) return;

            MiniSongList.UpdateLayout();
            if (MiniSongList.ItemContainerGenerator.ContainerFromItem(match) is FrameworkElement container)
            {
                container.BringIntoView();   // 已在可视区时无动作，仅在移出可视区时滚动
            }
            else
            {
                MiniSongList.ScrollIntoView(match);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => HidePlayer();
    }
}
