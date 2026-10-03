using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GameMusicPlayer.Services
{
    public class KeySimulator
    {
        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        #endregion

        private static readonly Dictionary<string, ushort> NoteToVkMap = new Dictionary<string, ushort>
        {
            { "z", 0x5A }, { "x", 0x58 }, { "c", 0x43 }, { "v", 0x56 },
            { "b", 0x42 }, { "n", 0x4E }, { "m", 0x4D }, { ",", 0xBC },
            { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 },
            { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 },
        };

        private static readonly Dictionary<string, ushort> NoteToScanMap = new Dictionary<string, ushort>();

        static KeySimulator()
        {
            foreach (var kv in NoteToVkMap)
            {
                NoteToScanMap[kv.Key] = (ushort)MapVirtualKey(kv.Value, 0);
            }
        }

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        public void KeyDown(string key)
        {
            if (NoteToVkMap.TryGetValue(key.ToLower(), out ushort vkCode))
            {
                ushort scanCode = NoteToScanMap[key.ToLower()];
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vkCode,
                            wScan = scanCode,
                            dwFlags = KEYEVENTF_KEYDOWN | KEYEVENTF_SCANCODE,
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                };
                SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            }
        }

        public void KeyUp(string key)
        {
            if (NoteToVkMap.TryGetValue(key.ToLower(), out ushort vkCode))
            {
                ushort scanCode = NoteToScanMap[key.ToLower()];
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vkCode,
                            wScan = scanCode,
                            dwFlags = KEYEVENTF_KEYUP | KEYEVENTF_SCANCODE,
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                };
                SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            }
        }

        public void PressKey(string key, int durationMs = 100)
        {
            KeyDown(key);
            System.Threading.Thread.Sleep(durationMs);
            KeyUp(key);
        }

        private void MouseAction(uint flags)
        {
            var input = new INPUT
            {
                type = 0,
                u = new INPUTUNION
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0, dy = 0, mouseData = 0,
                        dwFlags = flags,
                        time = 0, dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public void MouseLeftDown() => MouseAction(MOUSEEVENTF_LEFTDOWN);
        public void MouseLeftUp() => MouseAction(MOUSEEVENTF_LEFTUP);
        public void MouseRightDown() => MouseAction(MOUSEEVENTF_RIGHTDOWN);
        public void MouseRightUp() => MouseAction(MOUSEEVENTF_RIGHTUP);
        public void MouseMiddleDown() => MouseAction(MOUSEEVENTF_MIDDLEDOWN);
        public void MouseMiddleUp() => MouseAction(MOUSEEVENTF_MIDDLEUP);

        public void PlayNote(Models.Note note, int durationMs = -1, Func<bool>? holdWhile = null)
        {
            int hold = durationMs > 0 ? durationMs : note.Duration;
            if (hold < 1) hold = 1;

            if (string.IsNullOrEmpty(note.Key))
            {
                SleepResponsive(hold, holdWhile);
                return;
            }

            // 鼠标键组合原样回放：左键=降调、右键=升调、中键=半音辅助键，可组合按住
            bool left = note.IsFlat;
            bool right = note.IsSharp;
            bool middle = note.IsNatural;

            if (left) MouseLeftDown();
            if (right) MouseRightDown();
            if (middle) MouseMiddleDown();
            if (left || right || middle) System.Threading.Thread.Sleep(10);

            KeyDown(note.Key);
            SleepResponsive(hold, holdWhile);   // 分片睡眠：暂停/停止 25ms 内即时生效
            KeyUp(note.Key);

            if (left || right || middle) System.Threading.Thread.Sleep(10);
            if (left) MouseLeftUp();
            if (right) MouseRightUp();
            if (middle) MouseMiddleUp();
        }

        private static void SleepResponsive(int ms, Func<bool>? holdWhile)
        {
            // 使用Stopwatch精确计时，避免Thread.Sleep精度问题导致播放加速
            var stopwatch = Stopwatch.StartNew();
            long targetTicks = (long)ms * Stopwatch.Frequency / 1000;
            
            while (stopwatch.ElapsedTicks < targetTicks)
            {
                if (holdWhile != null && !holdWhile()) break;
                
                // 计算剩余时间
                long remainingTicks = targetTicks - stopwatch.ElapsedTicks;
                long remainingMs = remainingTicks * 1000 / Stopwatch.Frequency;
                
                if (remainingMs <= 0) break;
                
                // 短时间等待，保持响应性
                int sleepMs = (int)Math.Min(10, remainingMs);
                if (sleepMs > 0)
                {
                    System.Threading.Thread.Sleep(sleepMs);
                }
                else
                {
                    // 对于非常短的等待，使用SpinWait
                    System.Threading.Thread.SpinWait(100);
                }
            }
        }
    }
}