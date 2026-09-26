using System;
using Ember.Basic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.UI;
using Game.Module;
using Game.Narrative;
using Game.NovelSave;

namespace Game.UI
{
    /// <summary>UI/state orchestration stays in the business assembly; save/runtime modules do not depend on pages.</summary>
    internal static class NovelSaveUI
    {
        internal static NovelSaveModule Module => EmberModuleCollector.Instance.TryGetModule(out NovelSaveModule module) ? module : null;
        private static bool _opening;
        private static bool _loading;
        private static bool _cancelRequested;
        internal static bool IsLoading => _loading;
        internal static bool IsStarting { get; private set; }
        internal sealed class RestoreLoadingRequest
        {
            internal bool ShowProgress { get; }
            internal float Progress { get; set; }
            internal RestoreLoadingRequest(bool showProgress = false) { ShowProgress = showProgress; }
        }
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _opening = false; _loading = false; _cancelRequested = false; IsStarting = false; }
        internal static void CancelLoad()
        {
            if (_loading) _cancelRequested = true;
            Module?.CancelLoad();
        }
        internal static void Open()
        {
            if (_opening || _loading || Module == null) return;
            _opening = true;
            EUIManager.Instance.ShowPopup(GamePages.EUINovelSavePage, null, page => { if (page == null) _opening = false; });
        }
        internal static void Closed() { _opening = false; }
        internal static NarrativeTableCatalog Catalog()
        {
            if (!EmberModuleCollector.Instance.TryGetModule(out GameTableModule table)) throw new InvalidOperationException("配表模块未装配");
            if (!table.IsReady && table.LastLoadResult != null) throw new InvalidOperationException("小说配表加载失败");
            return table.IsReady ? table.NovelCatalog : null;
        }
        internal static void StartNewGame()
            => StartFromEntry(new NovelNewGameRequest());
#if UNITY_EDITOR
        internal static void StartEntryFromEditor(NovelNewGameRequest request)
        {
            if (NarrativeObservation.Current != null || !EUIViewEngine.Instance.ActivePages.Any(p =>
                p.EUIPageDef == GamePages.EUIMainPage && p.IsOpened))
                throw new InvalidOperationException("请先从 FrameworkScene 运行并返回主菜单，再启动指定入口。");
            StartFromEntry(request);
        }
#endif
        internal static void StartFromEntry(NovelNewGameRequest request)
        {
            if (_loading || Module?.IsBusy == true) return;
            _loading = true; IsStarting = true;
            StartCovered(request).Forget();
        }
        private static async UniTask StartCovered(NovelNewGameRequest request)
        {
            var token = UnityEngine.Application.exitCancellationToken;
            NovelSession session = null;
            NarrativeModule owner = null;
            var loading = new RestoreLoadingRequest(true);
            try
            {
                await EUIManager.Instance.RunWithLoadingAsync(GamePages.EUILoadingPage, loading, async () =>
                {
                    loading.Progress = .15f;
                    Module?.ReportMessage("");
                    GameLauncher.Instance.Fsm.TransitionTo<GameplayState>(request);
                    await UniTask.WaitUntil(() => EmberModuleCollector.Instance.TryGetModule(out owner) && owner.Session != null,
                        cancellationToken: token);
                    session = owner.Session;
                    loading.Progress = .45f;
                    await UniTask.WaitUntil(() => session.IsDisposed || session.Snapshot.State == NarrativeState.Faulted ||
                        session.IsReady && (session.Snapshot.State == NarrativeState.Revealing ||
                        session.Snapshot.State == NarrativeState.AwaitingAdvance || session.Snapshot.State == NarrativeState.AwaitingChoice ||
                        session.Snapshot.State == NarrativeState.Ended) &&
                        (session.Snapshot.Wait & (NarrativeWait.Resource | NarrativeWait.Presentation | NarrativeWait.Transition)) == 0 &&
                        EUIViewEngine.Instance.ActivePages.Any(p => p.EUIPageDef == GamePages.EUINovelReaderPage &&
                            p.IsOpened && ReferenceEquals(p.Logic, session.View)), cancellationToken: token);
                    if (session.IsDisposed || session.Snapshot.State == NarrativeState.Faulted)
                    {
                        string error = session.Snapshot.Error?.ToString() ?? "新游戏会话已结束";
                        GameLauncher.Instance.Fsm.TransitionTo<MainState>();
                        await UniTask.WaitUntil(() => EUIViewEngine.Instance.ActivePages.Any(p =>
                            p.EUIPageDef == GamePages.EUIMainPage && p.IsOpened), cancellationToken: token);
                        throw new InvalidOperationException(error);
                    }
                    // Freeze the first prepared reading point while the existing Loading owner renders and exits.
                    owner.PreparingEntryUnderCover = false;
                    session.Pause("SceneLoading");
                    // Only report completion once the actual first reading frame is ready under the curtain.
                    loading.Progress = 1f;
                    await UniTask.Delay(TimeSpan.FromSeconds(.2), ignoreTimeScale: true, cancellationToken: token);
                }, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Module?.ReportMessage("新游戏加载失败：" + ex.Message); }
            finally
            {
                if (owner != null) owner.PreparingEntryUnderCover = false;
                session?.Resume("SceneLoading");
                _loading = false; IsStarting = false;
                // 失败或取消时主菜单可能已在遮挡下打开；解除忙碌后同步恢复其按钮状态。
                if (!token.IsCancellationRequested)
                {
                    var save = Module;
                    save?.ReportMessage(save.Message);
                }
            }
        }
        internal static void Load(int slot, Action<string> failed = null)
        {
            var save = Module; if (save == null || _loading || save.IsBusy) return;
            _loading = true; _cancelRequested = false;
            LoadCovered(save, slot, failed).Forget();
        }
        private static async UniTask LoadCovered(NovelSaveModule save, int slot, Action<string> failed)
        {
            var token = UnityEngine.Application.exitCancellationToken;
            EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative);
            var original = narrative?.Session;
            original?.Pause("RestoreCover");
            save.ReportMessage("正在准备读档…");
            bool accepted = false;
            string error = null;
            NovelSession restored = null;
            try
            {
                await EUIManager.Instance.RunWithLoadingAsync(GamePages.EUILoadingPage, new RestoreLoadingRequest(), async () =>
                {
                    if (_cancelRequested) throw new OperationCanceledException();
                    var prepared = new UniTaskCompletionSource();
                    accepted = save.BeginLoad(slot, Catalog, candidate =>
                    {
                        restored = candidate;
                        candidate.Pause("RestoreCover");
                        if (EmberModuleCollector.Instance.TryGetModule(out NarrativeModule active) && active.Session != null)
                        {
                            active.CommitCandidate(candidate); save.Track(candidate); EUIManager.Instance.CloseAllPopups();
                            candidate.Resume("ReaderModal");
                        }
                        else
                        {
                            EUIManager.Instance.CloseAllPopups();
                            GameLauncher.Instance.Fsm.TransitionTo<GameplayState>(new NovelPreparedGameRequest(candidate));
                        }
                        prepared.TrySetResult();
                    }, message => prepared.TrySetException(new InvalidOperationException(message)),
                        () => prepared.TrySetCanceled());
                    if (!accepted) throw new InvalidOperationException(save.Message);
                    try
                    {
                        await prepared.Task.AttachExternalCancellation(token);
                        await UniTask.WaitUntil(() => restored.IsReady || restored.IsDisposed, cancellationToken: token);
                        if (restored.IsDisposed) throw new InvalidOperationException(save.Message ?? "恢复失败，存档文件已保留");
                        // Session readiness alone does not mean the reader's opening transition has finished.
                        await UniTask.WaitUntil(() => restored.IsDisposed || EUIViewEngine.Instance.ActivePages.Any(p =>
                            p.EUIPageDef == GamePages.EUINovelReaderPage && p.IsOpened &&
                            ReferenceEquals(p.Logic, restored.View)), cancellationToken: token);
                        if (restored.IsDisposed) throw new InvalidOperationException(save.Message ?? "恢复失败，存档文件已保留");
                        // Publish the terminal state before completing the operation. The Loading owner then
                        // allows destination render frames before starting its exit animation.
                        save.ReportMessage("读档成功");
                    }
                    catch
                    {
                        // An irreversible commit failure returns to the menu; keep its replacement covered.
                        if (restored?.IsDisposed == true && original?.IsDisposed != false)
                            await UniTask.WaitUntil(() => EUIViewEngine.Instance.ActivePages.Any(p =>
                                p.EUIPageDef == GamePages.EUIMainPage && p.IsOpened), cancellationToken: token);
                        throw;
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested) save.ReportMessage("已取消读档，原进度保留");
            }
            catch (Exception ex) { error = ex.Message; }
            finally
            {
                if (accepted) save.CancelLoad();
                original?.Resume("RestoreCover"); restored?.Resume("RestoreCover");
                _loading = false;
                if (!token.IsCancellationRequested) save.ReportMessage(error ?? save.Message);
            }
            // A failure chooser opens only after the curtain has finished leaving.
            if (error != null && !token.IsCancellationRequested) failed?.Invoke(error);
        }
        /// <summary>
        /// 直接读取最新档位（含自动槽与快速槽）；只有失败时才打开槽位页。
        /// 主菜单的「继续游戏」用它续读最新进度；「读取存档」另走 <see cref="Open"/> 直接开槽位页。
        /// </summary>
        internal static void Continue()
        {
            var save = Module; if (save == null) return;
            // Go directly through preparation/loading; only a failure opens the slot chooser.
            // This also offers other quick/auto slots when no manual slot exists.
            Load(save.Store.Latest, _ => Open());
        }
    }
}
