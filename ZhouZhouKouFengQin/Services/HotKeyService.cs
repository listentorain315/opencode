using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GameMusicPlayer.Services
{
    public class HotKeyService : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_NONE = 0x0000;

        #endregion

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private readonly int _previousSongHotKeyId = 9004;
        private readonly int _playPauseHotKeyId = 9001;
        private readonly int _nextSongHotKeyId = 9002;
        private readonly int _toggleMiniHotKeyId = 9003;
        private bool _isRegistered;

        public event EventHandler? PreviousSongPressed;
        public event EventHandler? PlayPausePressed;
        public event EventHandler? NextSongPressed;
        /// <summary>` 键（Tab 上方）：显示/隐藏小助手</summary>
        public event EventHandler? ToggleMiniPressed;

        public HotKeyService(Window window)
        {
            window.Loaded += (s, e) => RegisterHotKeys(window);
        }

        private void RegisterHotKeys(Window window)
        {
            if (_isRegistered) return;

            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, _previousSongHotKeyId, MOD_NONE, 0x76);   // F7 上一首
            RegisterHotKey(_windowHandle, _playPauseHotKeyId, MOD_NONE, 0x77);      // F8 播放/暂停
            RegisterHotKey(_windowHandle, _nextSongHotKeyId, MOD_NONE, 0x78);       // F9 下一首
            RegisterHotKey(_windowHandle, _toggleMiniHotKeyId, MOD_NONE, 0xC0);     // ` 键（OemTilde）

            _isRegistered = true;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == _previousSongHotKeyId)
                {
                    PreviousSongPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == _playPauseHotKeyId)
                {
                    PlayPausePressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == _nextSongHotKeyId)
                {
                    NextSongPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == _toggleMiniHotKeyId)
                {
                    ToggleMiniPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_isRegistered) return;
            _source?.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, _previousSongHotKeyId);
            UnregisterHotKey(_windowHandle, _playPauseHotKeyId);
            UnregisterHotKey(_windowHandle, _nextSongHotKeyId);
            UnregisterHotKey(_windowHandle, _toggleMiniHotKeyId);
        }
    }
}