// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 Image。
    /// 继承自 <see cref="Image"/>，额外提供：
    /// <list type="bullet">
    ///   <item>Sprite 数组 + 索引切换（配合 Animation 做序列帧）</item>
    ///   <item>内置帧动画支持（fps / loop / delay）</item>
    ///   <item>不规则点击区域（alphaHitTestMinimumThreshold）</item>
    ///   <item>自动 SetNativeSize</item>
    /// </list>
    /// </summary>
    [EUIExtension(typeof(Image))]
    [AddComponentMenu("UI/EUI/Image Ex")]
    public class EUIImageEx : Image
    {
        #region 编辑器面板参数

        [FoldoutGroup("精灵数组")]
        [SerializeField]
        [LabelText("精灵列表")]
        [Tooltip("可切换的精灵数组，按索引显示")]
        private Sprite[] _spriteArray;

        [FoldoutGroup("精灵数组")]
        [SerializeField]
        [LabelText("当前索引")]
        private int _spriteIndex;

        [FoldoutGroup("帧动画")]
        [SerializeField]
        [LabelText("启用帧动画")]
        private bool _animated;

        [FoldoutGroup("帧动画")]
        [SerializeField]
        [ShowIf("_animated")]
        [LabelText("帧率")]
        private int _fps = 25;

        [FoldoutGroup("帧动画")]
        [SerializeField]
        [ShowIf("_animated")]
        [LabelText("循环间隔")]
        [Tooltip("每轮动画结束后的等待时间（秒），0 = 无间隔立即循环")]
        private float _delay;

        [FoldoutGroup("帧动画")]
        [SerializeField]
        [ShowIf("_animated")]
        [LabelText("只播放一次")]
        private bool _playOnce;

        [FoldoutGroup("帧动画")]
        [SerializeField]
        [ShowIf("_animated")]
        [LabelText("播放速度")]
        private float _playbackSpeed = 1f;

        [FoldoutGroup("点击区域")]
        [SerializeField]
        [LabelText("不规则点击")]
        [Tooltip("启用后根据像素透明度判定点击")]
        private bool _irregularClickArea;

        [FoldoutGroup("点击区域")]
        [SerializeField]
        [ShowIf("_irregularClickArea")]
        [LabelText("透明度阈值")]
        [Range(0f, 1f)]
        [Tooltip("像素 alpha 低于此值时不响应点击")]
        private float _hitMinimalAlpha = 0.5f;

        [FoldoutGroup("布局")]
        [SerializeField]
        [LabelText("保持原始尺寸")]
        [Tooltip("切换精灵后自动调用 SetNativeSize")]
        private bool _keepNativeSize;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private float _accumulatedTime;
        private bool _delayStarted;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            if (_irregularClickArea)
                alphaHitTestMinimumThreshold = _hitMinimalAlpha;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _accumulatedTime = 0;
            if (_animated)
                _spriteIndex = 0;
            RefreshSpriteState();
        }

        private void Update()
        {
            if (!_animated)
                return;

            var tpf = 1f / (_fps * _playbackSpeed);
            var idx = _spriteIndex;
            var cnt = _spriteArray != null ? _spriteArray.Length : 0;
            if (cnt == 0)
                return;

            _accumulatedTime += Time.deltaTime;
            while (!_delayStarted && _accumulatedTime >= tpf)
            {
                idx++;
                _accumulatedTime -= tpf;

                if (_playOnce)
                {
                    if (idx >= cnt) idx = cnt - 1;
                }
                else
                {
                    if (_delay > 0 && idx >= cnt)
                    {
                        idx = 0;
                        _delayStarted = true;
                        _accumulatedTime = 0;
                    }
                }
            }
            while (_delayStarted && _accumulatedTime >= tpf)
            {
                if (_accumulatedTime < _delay)
                    return;
                _accumulatedTime -= _delay;
                _delayStarted = false;
            }
            idx = idx % cnt;

            SpriteIndex = idx;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>当前精灵索引</summary>
        public int SpriteIndex
        {
            get => _spriteIndex;
            set
            {
                if (_spriteIndex != value)
                {
                    _spriteIndex = value;
                    RefreshSpriteState();
                }
            }
        }

        /// <summary>精灵数组</summary>
        public Sprite[] SpriteArray
        {
            get => _spriteArray;
            set { _spriteArray = value; RefreshSpriteState(); }
        }

        /// <summary>是否保持原始尺寸</summary>
        public bool KeepNativeSize
        {
            get => _keepNativeSize;
            set
            {
                if (_keepNativeSize != value)
                {
                    _keepNativeSize = value;
                    if (value) SetNativeSize();
                }
            }
        }

        /// <summary>播放速度倍率</summary>
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = value;
        }

        /// <summary>是否启用帧动画</summary>
        public bool Animated
        {
            get => _animated;
            set => _animated = value;
        }

        /// <summary>刷新精灵显示</summary>
        public void RefreshSpriteState()
        {
            if (_spriteArray != null && _spriteIndex < _spriteArray.Length && _spriteIndex >= 0)
            {
                overrideSprite = _spriteArray[_spriteIndex];
                if (_keepNativeSize)
                    SetNativeSize();
            }
        }

        #endregion
    }
}
