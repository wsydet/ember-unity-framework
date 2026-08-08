/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : EmberLoading
 * page name    : Loading
 * create date  : 2026/8/8 11:45:26
==============================================================*/
using Ember.Basic;
using Ember.Core;
using Ember.Scene;
using UnityEngine;

namespace Ember.UI.Pages
{
    public partial class EmberLoading
    {
        private const string TAG = LogTags.EmberUI;
        private const string FORMAT = "加载中... {0}%";

        private bool _isActive;

        // ── 生命周期钩子 ──

        public override void OnInit()
        {
            base.OnInit();
            NeedUpdate = true;

            EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoadStart, OnSceneLoadStart);
            EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoadDone, OnSceneLoadDone);
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public override void OnDispose()
        {
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadStart, OnSceneLoadStart);
            EmberEventBus.Unsubscribe(EmberBroadcastEvent.SceneLoadDone, OnSceneLoadDone);
            base.OnDispose();
        }

        public override void OnUpdate()
        {
            if (!_isActive) return;

            float progress = EmberSceneManager.Instance.DisplayProgress;
            _ProgressBar.fillAmount = progress;
            _StatusText.text = string.Format(FORMAT, Mathf.RoundToInt(progress * 100f));
        }

        // ── 事件响应 ──

        private void OnSceneLoadStart()
        {
            _isActive = true;
            EmberDebug.LogEvent(TAG, "Scene loading started.");
        }

        private void OnSceneLoadDone()
        {
            EmberDebug.LogEvent(TAG, "Scene loading done.");
            EmberUIPageRouter.Instance.ClosePage(Page);
        }
    }
}
