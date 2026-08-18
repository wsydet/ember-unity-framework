using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Ember.Basic;
using Ember.Core;
using Ember.Scene;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Module
{
    /// <summary>
    /// 全局灯光模块 —— 全局一套 Light2D，按场景名切换灯光数值，DOTween 平滑过渡。
    ///
    /// <b>定位：</b>
    /// 作为 <see cref="IEmberModule"/>（Phase = <see cref="ModulePhase.Global"/>），
    /// Init 状态启动、游戏退出时销毁。监听 <see cref="EmberBroadcastEvent.SceneLoaded"/>，
    /// 读取 <see cref="EmberSceneManager.CurrentScene"/> 获取场景名，查找对应
    /// <see cref="SceneLightProfile"/> 执行灯光切换 —— 不直连 Unity 的 SceneManager。
    ///
    /// <b>启用：</b>
    /// 本模块 <see cref="Enabled"/> 默认 false（关闭）。启用时改为返回 true，
    /// 并在 OnInit 后调用 <c>Initialize(灯光列表, 配置资产)</c>。
    /// </summary>
    public class GlobalLightModule : EmberSingleton<GlobalLightModule>, IEmberModule
    {
        private const string TAG = LogTags.Game + "." + nameof(GlobalLightModule);

        /// <summary>模块是否启用。默认关闭，需要时改为返回 true。</summary>
        public bool Enabled => false;

        public int Phase => ModulePhase.Global;

        #region 内部参数

        private List<Light2D> _lights = new();
        private GlobalLightConfig _config;
        private IDisposable _sceneLoadedSub;
        private string _currentSceneName = "";
        private bool _isFirstTransition = true;

        #endregion

        // ============================================================

        #region 生命周期

        void IEmberModule.OnInit()
        {
            _currentSceneName = "";
            _isFirstTransition = true;

            // 监听场景加载完成事件（状态机大场景切换，静默流送不广播此事件）
            _sceneLoadedSub = EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoaded, OnSceneLoaded);
            EmberDebug.LogInit(TAG, "GlobalLightModule initialized.");
        }

        void IEmberModule.OnDestroy()
        {
            _sceneLoadedSub?.Dispose();
            _sceneLoadedSub = null;
            EmberDebug.LogCleanup(TAG, "GlobalLightModule destroyed.");
        }

        void IEmberModule.ResetModuleData()
        {
            _currentSceneName = "";
            _isFirstTransition = true;
        }

        #endregion

        // ============================================================

        #region 内部方法

        private void OnSceneLoaded()
        {
            string sceneName = EmberSceneManager.Instance.CurrentScene;
            if (string.IsNullOrEmpty(sceneName)) return;
            TransitionToSceneLight(sceneName).Forget();
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 用灯光组件列表 + 配置资产初始化。必须在 OnInit 后调用一次。
        /// </summary>
        /// <param name="lights">全局场景中的 Light2D 组件（仅一套，所有场景共用）</param>
        /// <param name="config">灯光配置资产（场景档案 + 过渡参数）</param>
        public void Initialize(List<Light2D> lights, GlobalLightConfig config)
        {
            if (lights == null || config == null)
            {
                EmberDebug.LogError(TAG, "GlobalLightModule.Initialize: lights or config is null.");
                return;
            }

            _lights = lights;
            _config = config;

            // 初始化时将所有灯光设为全黑，等待首次场景切换点亮
            foreach (var light in _lights)
            {
                if (light != null)
                {
                    light.intensity = 0f;
                    light.gameObject.SetActive(true);
                }
            }

            EmberDebug.LogInit(TAG,
                $"GlobalLightModule ready: {_lights.Count} lights, {config.sceneLightProfiles.Count} profiles.");
        }

        /// <summary>
        /// 切换灯光到指定场景的配置（DOTween 平滑渐变）。
        /// </summary>
        /// <param name="sceneName">目标场景名</param>
        /// <param name="overrideDuration">覆盖过渡时长，负数表示用默认时长</param>
        public async UniTask TransitionToSceneLight(string sceneName, float overrideDuration = -1f)
        {
            if (_config == null) return;

            float duration = overrideDuration >= 0f ? overrideDuration : _config.defaultTransitionDuration;

            SceneLightProfile profile = _config.GetProfile(sceneName);
            if (profile == null)
            {
                EmberDebug.LogWarning(TAG, $"GlobalLightModule: no light profile for scene '{sceneName}'.");
                return;
            }

            // 非首次且目标场景与当前一致 → 跳过
            if (sceneName == _currentSceneName && !_isFirstTransition)
                return;

            // 首次加载：Instant 模式瞬间完成（duration = 0）
            if (_isFirstTransition && _config.firstTransitionMode == FirstTransitionMode.Instant)
                duration = 0f;

            var tasks = new List<UniTask>();

            for (int i = 0; i < _lights.Count; i++)
            {
                var light = _lights[i];
                if (light == null || i >= profile.lightStates.Count) continue;

                if (!light.gameObject.activeSelf)
                    light.gameObject.SetActive(true);

                float targetIntensity = profile.lightStates[i].intensity;
                Color targetColor = profile.lightStates[i].color;

                tasks.Add(DOTween.To(() => light.intensity, x => light.intensity = x, targetIntensity, duration)
                    .SetEase(Ease.InOutSine).ToUniTask());
                tasks.Add(DOTween.To(() => light.color, x => light.color = x, targetColor, duration)
                    .SetEase(Ease.InOutSine).ToUniTask());
            }

            _currentSceneName = sceneName;
            _isFirstTransition = false;

            await UniTask.WhenAll(tasks);
        }

        /// <summary>切换灯光的 fire-and-forget 版本。</summary>
        public void TransitionToSceneLightFireAndForget(string sceneName, float overrideDuration = -1f)
            => TransitionToSceneLight(sceneName, overrideDuration).Forget();

        #endregion
    }
}
