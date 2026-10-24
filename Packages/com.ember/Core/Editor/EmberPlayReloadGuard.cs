using Ember.Basic;
using UnityEditor;
using UnityEditor.Compilation;
using System.Threading.Tasks;

namespace Ember.Core.Editor
{
    /// <summary>编译开始先异步收束再退出 Play；直接重载仅同步兜底，不恢复旧会话。</summary>
    [InitializeOnLoad]
    internal static class EmberPlayReloadGuard
    {
        private const string PENDING = "Ember.PlayReload.StopPending";
        private static Task _draining;
        private static double _started;
        private static bool _timeoutReported;
        private static bool _failureReported;

        static EmberPlayReloadGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            CompilationPipeline.compilationStarted += BeforeCompilation;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (SessionState.GetBool(PENDING, false)) EditorApplication.delayCall += FinishExit;
        }

        private static void BeforeCompilation(object context)
        {
            var launcher = GameLauncher.Active;
            if (!EditorApplication.isPlaying || launcher == null || _draining != null) return;
            // Acquire before the reload boundary, not from beforeAssemblyReload.
            EditorApplication.LockReloadAssemblies();
            _started = EditorApplication.timeSinceStartup;
            _timeoutReported = false;
            _failureReported = false;
            SessionState.SetBool(PENDING, true);
            _draining = launcher.ShutdownFrameworkAsync(EmberStopReason.ScriptReload);
            EditorApplication.update += PollDrain;
        }

        private static void PollDrain()
        {
            if (!_draining.IsCompleted)
            {
                if (!_timeoutReported && EditorApplication.timeSinceStartup - _started > 10)
                {
                    _timeoutReported = true;
                    EmberDebug.LogError(LogTags.CoreGameLauncher,
                        "重载停止等待超时：仍保留所有权及临时重载屏障；不会提前释放锁或恢复业务。请检查未完成的停止任务。");
                }
                return;
            }
            if (_draining.IsFaulted || _draining.IsCanceled)
            {
                if (!_failureReported)
                {
                    _failureReported = true;
                    EmberDebug.LogError(LogTags.CoreGameLauncher,
                        "重载停止失败：保留临时重载屏障，不能确认所有权已释放。" + _draining.Exception);
                }
                return;
            }
            EditorApplication.update -= PollDrain;
            var completed = _draining; _draining = null;
            if (completed.IsFaulted) EmberDebug.LogError(LogTags.CoreGameLauncher, completed.Exception.ToString());
            EditorApplication.UnlockReloadAssemblies();
            // Exit in this domain when possible; the pending flag also covers a reload before delayCall.
            EditorApplication.delayCall += FinishExit;
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
