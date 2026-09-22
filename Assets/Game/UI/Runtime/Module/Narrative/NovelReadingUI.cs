using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.UI;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    internal static class NovelReadingUI
    {
        internal static float FontScale => new[] { .85f, 1f, 1.2f }[Mathf.Clamp(NovelSaveUI.Module?.Account?.FontSize ?? 1, 0, 2)];
        internal static void Open(EUIPageDef page, NovelSession session)
        {
            if (session == null || session.IsDisposed || session.DialogueHidden || session.Snapshot.PauseReasons.Count > 0) return;
            var pending = session.AcquirePause("ReadingPopupOpening");
            try
            {
                EUIManager.Instance.ShowPopup(page, session, opened =>
                {
                    pending.Dispose();
                    if (session.IsDisposed && opened != null) EUIManager.Instance.ClosePage(opened);
                });
            }
            catch { pending.Dispose(); throw; }
        }
        internal static async UniTask AfterClose(EUIPage page, NovelSession session, Action action)
        {
            EUIManager.Instance.ClosePage(page);
            await UniTask.WaitUntil(() => session.IsDisposed || !EUIViewEngine.Instance.ActivePages.Contains(page),
                cancellationToken: Application.exitCancellationToken);
            // Underlying reader resumes as the close transaction completes.
            await UniTask.NextFrame(cancellationToken: Application.exitCancellationToken);
            if (!session.IsDisposed) action();
        }
    }
}
