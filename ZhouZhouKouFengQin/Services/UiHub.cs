using System;
using System.Threading.Tasks;

namespace GameMusicPlayer.Services
{
    /// <summary>
    /// 站内 UI 桥：用窗口内覆盖层 / Toast 替代 MessageBox 弹窗。
    /// </summary>
    public static class UiHub
    {
        /// <summary>title, message, needConfirm → 用户是否确认</summary>
        public static Func<string, string, bool, Task<bool>>? ShowOverlay;

        public static Action<string>? PushToast;

        /// <summary>title, scriptText → 站内脚本查看器</summary>
        public static Action<string, string>? ShowScript;

        public static Task<bool> ConfirmAsync(string title, string message)
        {
            if (ShowOverlay == null) return Task.FromResult(false);
            return ShowOverlay(title, message, true);
        }

        public static Task InfoAsync(string title, string message)
        {
            if (ShowOverlay == null) return Task.CompletedTask;
            return ShowOverlay(title, message, false);
        }

        public static void Notify(string message) => PushToast?.Invoke(message);
    }
}
