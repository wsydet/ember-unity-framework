using Ember.Basic;
using UnityEditor;

namespace Ember.Core.Editor
{
    /// <summary>Play 会话不跨程序集重载恢复。旧域同步中止，新域完成退出。</summary>
    [InitializeOnLoad]
    internal static class EmberPlayReloadGuard
    {
        private const string PENDING = "Ember.PlayReload.StopPending";

        static EmberPlayReloadGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (SessionState.GetBool(PENDING, false)) EditorApplication.delayCall += FinishExit;
        }

        private static void BeforeReload()
        {
            var launcher = GameLauncher.Active;
            if (!EditorApplication.isPlaying || launcher == null) return;
            SessionState.SetBool(PENDING, true);
            EmberDebug.LogWarning(LogTags.CoreGameLauncher,
                "Ember 不支持 Play 脚本热重载：当前会话已终止，将退出 Play。未自动保存；请重新进入 Play。");
            launcher.ShutdownFramework(EmberStopReason.ScriptReload);
            // The old-domain stop request races the restored editor lifecycle in Unity 6000.5.
            // Only the new-domain callback requests Play exit.
        }

        private static void FinishExit()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            SessionState.SetBool(PENDING, false);
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
                GameLauncher.Active?.ShutdownFramework();
            if (change == PlayModeStateChange.EnteredEditMode)
                SessionState.SetBool(PENDING, false);
        }
    }
}
