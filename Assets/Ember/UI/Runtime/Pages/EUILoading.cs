/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : EmberLoading
 * page name    : EUILoadingPage
 * create date  : 2026/8/11 10:25:49
==============================================================*/

using Cysharp.Threading.Tasks;

using Ember.Core;
using Ember.Scene;
using Ember.UI;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Game.UI
{
    public partial class EUILoading
    {
        #region 内部参数

        private EUILoadingSettings _settings;

        // 假进度状态
        private float _fastElapsed;
        private float _tailElapsed;
        private float _displayProgress;
        private bool _inTailPhase;
        private bool _fakeComplete;

        #endregion

        // --------------------------------------------------------

        #region 生命周期钩子

        public override void OnInit()
        {
            base.OnInit();
            _settings = CustomSettings as EUILoadingSettings;
            NeedUpdate = true;
            ApplySettings();
        }

        public override void OnShow()
        {
            base.OnShow();
            _fastElapsed = 0f;
            _tailElapsed = 0f;
            _displayProgress = 0f;
            _inTailPhase = false;
            _fakeComplete = false;
            ApplySettings();
            EmberEventBus.OnNext(EUIEvents.LoadingFadeInStart);
        }

        public override void OnUpdate()
        {
            if (_settings == null || _fakeComplete) return;

            var sceneMgr = EmberSceneManager.Instance;
            bool realDone = sceneMgr != null && !sceneMgr.IsLoading;

            if (!_inTailPhase)
            {
                // Phase 1: 快充阶段 → 阈值
                _fastElapsed += Time.deltaTime;
                float fastT = Mathf.Clamp01(_fastElapsed / _settings.fastFillDuration);
                _displayProgress = fastT * _settings.fastFillThreshold;

                if (fastT >= 1f)
                {
                    if (realDone)
                    {
                        // 真实加载已完成 → 进入收尾
                        _inTailPhase = true;
                        _tailElapsed = 0f;
                    }
                    else
                    {
                        // 真实加载未完成 → 卡在阈值等待
                        _displayProgress = _settings.fastFillThreshold;
                    }
                }
            }

            if (_inTailPhase)
            {
                // Phase 2: 收尾阶段 → 当前进度平滑到 100%
                _tailElapsed += Time.deltaTime;
                float tailT = Mathf.Clamp01(_tailElapsed / _settings.tailDuration);
                _displayProgress = Mathf.Lerp(_settings.fastFillThreshold, 1f, tailT);

                if (tailT >= 1f)
                {
                    _displayProgress = 1f;
                    _fakeComplete = true;
                    EmberEventBus.OnNext(EUIEvents.LoadingFadeOutStart);
                }
            }

            SetProgress(_displayProgress);
        }

        public override void OnHide()
        {
            base.OnHide();
            NeedUpdate = false;
        }

        public override void OnReset()
        {
            base.OnReset();
            _fastElapsed = 0f;
            _tailElapsed = 0f;
            _displayProgress = 0f;
            _inTailPhase = false;
            _fakeComplete = false;
            SetProgress(0f);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 手动设置加载进度（0-1），更新进度条和数字。
        /// 如果不手动调用，OnUpdate 通过 timer 驱动假进度 0→1，不依赖真实加载进度。
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (_settings != null)
            {
                if (_settings.useProgressBar && Img_ProgressBar != null)
                {
                    Img_ProgressBar.fillAmount = progress;
                    Img_ProgressBar.gameObject.SetActive(true);
                }
                else if (Img_ProgressBar != null)
                {
                    Img_ProgressBar.gameObject.SetActive(false);
                }

                if (_settings.useProgressNumber && Txt_ProgressNum != null)
                {
                    Txt_ProgressNum.text = $"{(int)(progress * 100)}%";
                    Txt_ProgressNum.gameObject.SetActive(true);
                }
                else if (Txt_ProgressNum != null)
                {
                    Txt_ProgressNum.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 展示进度的平滑收尾时长（秒）。
        /// <see cref="EUIManager.TransitionSceneWithLoading"/> 在关闭页面前等待此时长。
        /// 假进度模式下返回 0，事件驱动关闭。
        /// </summary>
        public override float SmoothTailDuration => 0f;

        /// <summary>假进度是否已完成</summary>
        public bool IsFakeComplete => _fakeComplete;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void ApplySettings()
        {
            if (_settings == null) return;
            SetProgress(0f);

            // 初始化进度条组透明度（自定义动画接管前先隐藏）
            if (Cg_Progress != null)
                Cg_Progress.alpha = 0f;
        }

        #endregion

        // --------------------------------------------------------

        #region 自定义过渡动画

        /// <summary>
        /// 进度条组渐显：CanvasGroup alpha 0 → 1。
        /// 在 <see cref="EUIPage.PlayShow"/> 阶段被框架调用，
        /// 如果同时勾选了预设渐入渐出，则在预设动画之后执行。
        /// </summary>
        public override async UniTask OnCustomEnter()
        {
            if (_settings == null || Cg_Progress == null) return;

            var duration = _settings.customEnterDuration;
            if (duration <= 0f) { Cg_Progress.alpha = 1f; return; }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Cg_Progress.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            Cg_Progress.alpha = 1f;
            EmberEventBus.OnNext(EUIEvents.LoadingFadeInComplete);
        }

        /// <summary>
        /// 进度条组渐隐：CanvasGroup alpha 1 → 0。
        /// 在 <see cref="EUIPage.PlayHide"/> 阶段被框架调用，
        /// 如果同时勾选了预设渐入渐出，则在预设动画之前执行。
        /// </summary>
        public override async UniTask OnCustomExit()
        {
            if (_settings == null || Cg_Progress == null) return;

            var duration = _settings.customExitDuration;
            if (duration <= 0f) { Cg_Progress.alpha = 0f; return; }

            float elapsed = 0f;
            var startAlpha = Cg_Progress.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Cg_Progress.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            Cg_Progress.alpha = 0f;
            EmberEventBus.OnNext(EUIEvents.LoadingFadeOutComplete);
        }

        #endregion
    }
}
