// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>
    /// 业务 EUI 页面中的 SceneUI 宿主。所有气泡实例都会生成到指定 BubbleRoot 下。
    /// 页面层级与生命周期仍完全归业务 EUI 管理。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneUIPageHost : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField]
        private RectTransform _bubbleRoot;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private EmberSceneUIEngine _engine;
        private SceneUIContextHandle _contextHandle;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>全部 SceneUI View 的直接父节点。</summary>
        public RectTransform BubbleRoot => _bubbleRoot;

        /// <summary>读取当前宿主所绑定 Engine 的诊断快照。</summary>
        [NoGC]
        public bool TryGetDiagnostics(out SceneUIDiagnostics diagnostics)
        {
            if (_engine != null
                && !_engine.IsDisposed
                && _engine.IsContextValid(_contextHandle))
            {
                diagnostics = _engine.Diagnostics;
                return true;
            }

            diagnostics = default;
            return false;
        }

        /// <summary>校验 BubbleRoot 与所在页面 Canvas。</summary>
        [HasGC]
        public bool TryResolve(
            out RectTransform bubbleRoot,
            out Canvas canvas,
            out Camera uiCamera,
            out string error)
        {
            bubbleRoot = _bubbleRoot;
            canvas = null;
            uiCamera = null;

            if (!bubbleRoot)
            {
                error = "SceneUIPageHost has no BubbleRoot.";
                return false;
            }

            if (bubbleRoot != transform && !bubbleRoot.IsChildOf(transform))
            {
                error = "BubbleRoot must be the SceneUIPageHost transform or one of its children.";
                return false;
            }

            canvas = bubbleRoot.GetComponentInParent<Canvas>();
            if (!canvas)
            {
                error = "SceneUIPageHost must be placed under an EUI page Canvas.";
                return false;
            }

            Canvas[] nestedCanvases = bubbleRoot.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < nestedCanvases.Length; i++)
            {
                Canvas nestedCanvas = nestedCanvases[i];
                if (nestedCanvas && nestedCanvas != canvas && nestedCanvas.overrideSorting)
                {
                    error = $"BubbleRoot contains overrideSorting Canvas '{nestedCanvas.name}'. "
                            + "SceneUI children must remain inside the host page sorting layer.";
                    return false;
                }
            }

            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            error = null;
            return true;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        [NoGC]
        internal void Bind(EmberSceneUIEngine engine, SceneUIContextHandle contextHandle)
        {
            _engine = engine;
            _contextHandle = contextHandle;
        }

        [NoGC]
        internal void Unbind(EmberSceneUIEngine engine, SceneUIContextHandle contextHandle)
        {
            if (_engine != engine || _contextHandle != contextHandle)
                return;

            _engine = null;
            _contextHandle = default;
        }

        #endregion
    }
}
