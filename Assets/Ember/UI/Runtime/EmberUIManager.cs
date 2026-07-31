using System.Collections.Generic;
using Ember.Core;
using Ember.Resource;
using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// UI 界面层级预设值。决定界面的渲染顺序和输入优先级。
    /// 值越大，渲染越靠前。也可使用任意 int 值实现更细粒度的层级。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Normal     = 100,
        Popup      = 200,
        TopMost    = 300,
    }

    /// <summary>
    /// UI 管理器 —— 界面栈与层级管理。
    ///
    /// 参考 burner 的 <c>GameUIManager</c> + <c>BurnerUIManager</c>，核心设计：
    /// - 每个层级独立维护一个界面栈
    /// - 页面通过 <see cref="PageDef"/> 静态注册（类似 burner PageDef），
    ///   未来可由图形化编辑器自动生成
    /// - 通过 EmberResourceManager 加载预制体并实例化
    /// - 管理 IUIView 生命周期（OnOpen / OnPause / OnResume / OnClose）
    ///
    /// 使用方式：
    /// <code>
    /// // 静态注册表（手写或工具生成）
    /// public static class GamePages
    /// {
    ///     public static readonly PageDef MainMenu = new("ui/main_menu", UILayer.Normal);
    ///     public static readonly PageDef Settings = new("ui/settings",  UILayer.Popup);
    /// }
    ///
    /// // 打开页面
    /// EmberUIManager.Instance.Push(GamePages.Settings, args: null);
    ///
    /// // 返回键
    /// EmberUIManager.Instance.Pop(UILayer.Popup);
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.UI)]
    public class EmberUIManager : EmberSingleton<EmberUIManager>, IEmberManager
    {
        private const string TAG = LogTags.UIManager;
        #region 参数

        /// <summary>每个层级的界面栈。栈顶是当前可见的界面。</summary>
        private readonly Dictionary<int, Stack<IUIView>> _stacks = new();

        /// <summary>每个层级的 Canvas 根节点。</summary>
        private readonly Dictionary<int, Transform> _layerRoots = new();

        private bool _initialized;

        #endregion

        // ============================================================

        #region 外部方法

        // ======== Push ========

        /// <summary>
        /// 加载并显示一个 UI 界面，压入其所属层级的栈顶。
        ///
        /// 流程：
        /// 1. 暂停当前栈顶界面（OnPause）
        /// 2. 通过 EmberResourceManager 异步加载预制体
        /// 3. 实例化到对应 Canvas 层下
        /// 4. 调用新界面的 OnOpen
        /// 5. 压入该层栈顶
        /// </summary>
        /// <param name="page">页面定义（含预制体路径和层级）</param>
        /// <param name="args">传给 OnOpen 的参数，无参数时为 null</param>
        public void Push(PageDef page, object args = null)
        {
            if (!_initialized)
            {
                EmberDebug.LogError(TAG, "EmberUIManager is not initialized.");
                return;
            }

            if (page == null)
            {
                EmberDebug.LogError(TAG, "EmberUIManager.Push: page is null.");
                return;
            }

            int layer = page.Layer;
            EnsureLayerRoot(layer);

            PauseTopView(layer);

            EmberResourceManager.Instance.LoadAssetAsync<GameObject>(page.PrefabPath, prefab =>
            {
                if (prefab == null)
                {
                    EmberDebug.LogError(TAG, $"EmberUIManager.Push: failed to load prefab '{page.PrefabPath}'.");
                    return;
                }

                var instance = UnityEngine.Object.Instantiate(prefab, _layerRoots[layer]);
                instance.name = prefab.name;

                var view = instance.GetComponent<IUIView>();
                if (view == null)
                {
                    EmberDebug.LogError(TAG, 
                        $"EmberUIManager.Push: prefab '{page.PrefabPath}' " +
                        $"has no IUIView component. Push requires a MonoBehaviour implementing IUIView.");
                    UnityEngine.Object.Destroy(instance);
                    return;
                }

                GetOrCreateStack(layer).Push(view);
                view.OnOpen(args);
            });
        }

        // ======== Pop ========

        /// <summary>
        /// 关闭指定层级的栈顶界面。
        ///
        /// 流程：OnClose → 弹出栈 → Destroy → 恢复新的栈顶（OnResume）。
        /// </summary>
        public void Pop(int layer)
        {
            if (!TryPop(layer, out var view)) return;

            view.OnClose();
            DestroyView(view);
            ResumeTopView(layer);
        }

        /// <summary>
        /// 使用 <see cref="UILayer"/> 枚举的 Pop 重载。
        /// </summary>
        public void Pop(UILayer layer) => Pop((int)layer);

        // ======== CloseAll ========

        /// <summary>
        /// 关闭指定层级的所有界面。
        /// </summary>
        public void CloseAll(int layer)
        {
            if (!_stacks.TryGetValue(layer, out var stack)) return;

            while (stack.Count > 0)
            {
                var view = stack.Pop();
                view.OnClose();
                DestroyView(view);
            }
        }

        /// <summary>
        /// 使用 <see cref="UILayer"/> 枚举的 CloseAll 重载。
        /// </summary>
        public void CloseAll(UILayer layer) => CloseAll((int)layer);

        /// <summary>
        /// 关闭所有层级的所有界面。
        /// </summary>
        public void CloseAll()
        {
            foreach (var kvp in _stacks)
            {
                while (kvp.Value.Count > 0)
                {
                    var view = kvp.Value.Pop();
                    view.OnClose();
                    DestroyView(view);
                }
            }
        }

        // ======== 查询 ========

        /// <summary>
        /// 获取指定层级当前栈顶的界面，栈空返回 null。
        /// </summary>
        public IUIView GetTopView(int layer)
        {
            return TryPeek(layer, out var view) ? view : null;
        }

        /// <summary>
        /// 获取指定层级的界面数量。
        /// </summary>
        public int GetCount(int layer)
        {
            return _stacks.TryGetValue(layer, out var stack) ? stack.Count : 0;
        }

        /// <summary>
        /// 指定层级是否有界面在显示中。
        /// </summary>
        public bool HasView(int layer)
        {
            return GetCount(layer) > 0;
        }

        // ======== IEmberManager ========

        /// <summary>
        /// 由 ManagerCollector 自动调用的无参初始化。
        /// </summary>
        void IEmberManager.Init()
        {
            if (_initialized) return;

            if (GameLauncher.Instance.UIRoot == null)
            {
                EmberDebug.LogError(TAG, "GameBoot 下缺少 UIRoot 子节点，EmberUIManager 无法初始化。");
                return;
            }

            _initialized = true;
            EmberEventBus.OnNext(EmberBroadcastEvent.UIReady);
            EmberDebug.LogInit(TAG, "EmberUIManager initialized.");
        }

        /// <summary>
        /// 由 ManagerCollector 逆序调用的销毁逻辑。
        /// </summary>
        void IEmberManager.Destroy()
        {
            DestroyInternal();
        }

        #endregion

        // ============================================================

        #region 内部方法

        private void EnsureLayerRoot(int layer)
        {
            if (_layerRoots.ContainsKey(layer)) return;

            var go = new GameObject($"UI Layer - {layer}");
            go.transform.SetParent(GameLauncher.Instance.UIRoot.transform);
            _layerRoots[layer] = go.transform;
        }

        private Stack<IUIView> GetOrCreateStack(int layer)
        {
            if (!_stacks.TryGetValue(layer, out var stack))
            {
                stack = new Stack<IUIView>();
                _stacks[layer] = stack;
            }

            return stack;
        }

        private bool TryPeek(int layer, out IUIView view)
        {
            view = null;
            return _stacks.TryGetValue(layer, out var stack)
                && stack.TryPeek(out view);
        }

        private bool TryPop(int layer, out IUIView view)
        {
            view = null;
            return _stacks.TryGetValue(layer, out var stack)
                && stack.TryPop(out view);
        }

        private void PauseTopView(int layer)
        {
            if (TryPeek(layer, out var top))
                top.OnPause();
        }

        private void ResumeTopView(int layer)
        {
            if (TryPeek(layer, out var top))
                top.OnResume();
        }

        private void DestroyView(IUIView view)
        {
            if (view is MonoBehaviour mb && mb != null)
                UnityEngine.Object.Destroy(mb.gameObject);
        }

        /// <summary>
        /// EmberSingleton 销毁钩子。
        /// </summary>
        protected override void OnDestroy()
        {
            DestroyInternal();
        }

        /// <summary>
        /// 共享清理逻辑：关闭所有界面、广播 UIShutdown、重置状态。
        /// UIRoot 由 GameBoot 预置，不在此销毁。
        /// </summary>
        private void DestroyInternal()
        {
            CloseAll();
            EmberEventBus.OnNext(EmberBroadcastEvent.UIShutdown);
            _layerRoots.Clear();
            _initialized = false;
        }

        #endregion
    }
}
