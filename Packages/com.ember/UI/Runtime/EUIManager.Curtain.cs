using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using UnityEngine;

namespace Ember.UI
{
    public partial class EUIManager
    {
        #region 内部参数
        private EUICurtainSession _curtainSession;
        private EUIPage _curtainPage;
        #endregion

        #region 内部方法
        private async UniTask OpenCurtainAsync(EUICurtainSession session, EUIPageDef definition, object args,
            CancellationToken cancellation)
        {
            bool opened = false;
            try
            {
                EnqueueShow(definition, new EUICurtainOpenArgs { Session = session, Args = args }, page =>
                {
                    if (_curtainSession != session || session.IsReleased)
                    {
                        // An expired load must never close a page subsequently owned by another session.
                        if (page != null && page != _curtainPage) ClosePage(page);
                        return;
                    }
                    _curtainPage = page;
                    opened = true;
                }, isCurrent: () => _curtainSession == session && !session.IsReleased);
                await UniTask.WaitUntil(() => opened || session.IsReleased || (_curtainPage != null && _curtainPage.GameObject == null), cancellationToken: cancellation);
                if (session.IsReleased) return;
                var page = _curtainPage;
                if (page == null) throw new InvalidOperationException("Curtain page could not be opened.");
                if (page.GameObject == null) throw new OperationCanceledException("Curtain view was destroyed while opening.");
                if (session.AnimationError != null) throw session.AnimationError;
                // Unity 6 EndOfFrame runs after cameras/UI have rendered; NextFrame alone is not this barrier.
                await UniTask.WaitForEndOfFrame(cancellation);
                await UniTask.NextFrame(cancellationToken: cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (_curtainSession != session || page.GameObject == null || !page.IsOpened)
                    throw new OperationCanceledException("Curtain owner expired.");
                session.MarkCovered();
                MonitorCurtainAsync(session, page).Forget();
            }
            catch (Exception error) { session.Fail(error); }
        }

        private async UniTask MonitorCurtainAsync(EUICurtainSession session, EUIPage page)
        {
            while (!session.IsReleased)
            {
                if (!_initialized || page.GameObject == null ||
                    (session.State == EUICurtainState.Covered && !page.IsOpened))
                {
                    session.Fail(new OperationCanceledException("Curtain view was destroyed or externally closed."));
                    return;
                }
                await UniTask.Yield();
            }
        }

        private async UniTask RevealCurtainAsync(EUICurtainSession session)
        {
            if (_curtainSession != session) throw new OperationCanceledException("Curtain owner expired.");
            bool closed = false;
            var page = _curtainPage;
            if (page?.GameObject == null) throw new OperationCanceledException("Curtain view was destroyed.");
            ClosePageCore(page, null, true, onPageClosed: () => closed = true);
            await UniTask.WaitUntil(() => closed || session.IsReleased || page.GameObject == null);
            if (!closed) throw new OperationCanceledException("Curtain reveal did not finish.");
            EmberEventBus.OnNext(EUIEvents.LoadingFadeOutComplete);
        }

        private void ReleaseCurtain(EUICurtainSession session, bool success)
        {
            if (_curtainSession != session) return;
            var page = _curtainPage;
            try
            {
                if (!success && page != null)
                {
                    _context.RemoveTopMost(page);
                    if (page.GameObject == null)
                    {
                        _viewEngine.AbortCurtainPage(page);
                        _closeRequestedPages.Remove(page);
                        return;
                    }
                    Exception cleanupError = null;
                    foreach (var effect in page.GameObject.GetComponentsInChildren<IEUITransitionEffect>(true))
                    {
                        try { effect.HideAllImmediate(); }
                        catch (Exception error) { cleanupError ??= error; }
                    }
                    page.GameObject.SetActive(false);
                    _context.RemoveTopMost(page);
                    try { _viewEngine.AbortCurtainPage(page); }
                    finally { _closeRequestedPages.Remove(page); }
                    if (cleanupError != null) throw cleanupError;
                }
            }
            finally
            {
                _curtainSession = null;
                _curtainPage = null;
            }
        }
        #endregion

        #region 外部方法
        /// <summary>取得唯一会话。可立即 SetPaused；等待 Covered 后才执行遮挡下业务。并发拒绝，不排队或抢占。</summary>
        [HasGC]
        public EUICurtainSession BeginCurtain(EUIPageDef pageDef, object args = null,
            EUICurtainOptions options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initialized || _sessionStopped || pageDef == null) throw new InvalidOperationException("Curtain is unavailable.");
            if (pageDef.PageType != PageType.TopMost) throw new ArgumentException("Curtains require a TopMost page.", nameof(pageDef));
            if (_curtainSession != null || _context.FindOpenedPage(pageDef) != null)
                throw new InvalidOperationException("A curtain is already active.");
            var session = new EUICurtainSession(options, RevealCurtainAsync, ReleaseCurtain);
            session.PageAttached = page =>
            {
                if (_curtainSession == session && !session.IsReleased) _curtainPage = page;
            };
            _curtainSession = session;
            OpenCurtainAsync(session, pageDef, args, cancellationToken).Forget();
            return session;
        }

        /// <summary>闭幕并跨过实际渲染屏障。令牌仅用于取得覆盖；返回后必须显式揭幕或释放。</summary>
        [HasGC]
        public async UniTask<EUICurtainSession> CoverAsync(EUIPageDef pageDef, object args = null,
            EUICurtainOptions options = null, CancellationToken cancellationToken = default)
        {
            var session = BeginCurtain(pageDef, args, options, cancellationToken);
            await session.Covered;
            return session;
        }
        #endregion
    }
}
