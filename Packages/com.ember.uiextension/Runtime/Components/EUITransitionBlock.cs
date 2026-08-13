// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;
using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using DG.Tweening;

using Ember.Basic;
using Ember.Core;
using Ember.UI;

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 方块过渡动画组件，实现 <see cref="IEUITransitionEffect"/> 接口。
    /// 选中该子物体可在 Inspector 中配置所有参数并预览动画。
    /// </summary>
    public class EUITransitionBlock : MonoBehaviour, IEUITransitionEffect
    {
        private const string GROUP = "L1: 方块过渡";

        #region 编辑器面板参数

        [FoldoutGroup("$GROUP", Expanded = true)]
        [BoxGroup("$GROUP/预设", ShowLabel = false)]
        [Title("预设", "选一个预设自动填充进入/退出排布和动画类型。选「自定义」后手动微调。")]
        [OnValueChanged("ApplyPreset")]
        [ValueDropdown("GetPresetItems")]
        [SerializeField] private EUIBlockPreset _preset = EUIBlockPreset.SideWipe;

        [BoxGroup("$GROUP/方块外观", ShowLabel = false)]
        [Title("方块外观")]
        [Tooltip("方块颜色（未使用贴图时生效）")]
        [HideIf("_useBlockTexture")]
        [SerializeField] private Color _blockColor = Color.black;

        [BoxGroup("$GROUP/方块外观")]
        [Tooltip("勾选后把方块切成图片切片做转场，否则用纯色块。")]
        [InfoBox("图片宽高比需与「网格」分组里的「当前网格」一致，否则会被拉伸变形。", InfoMessageType.Warning, "_useBlockTexture")]
        [SerializeField] private bool _useBlockTexture = false;

        [BoxGroup("$GROUP/方块外观")]
        [Tooltip("方块贴图，会按当前网格切成小方块。")]
        [ShowIf("_useBlockTexture")]
        [SerializeField] private Texture2D _blockTexture;

        [BoxGroup("$GROUP/方块外观")]
        [Tooltip("方块动画类型")]
        [DisableIf("IsPresetLocked")]
        [SerializeField] private EUIBlockAnimationType _blockAnimation = EUIBlockAnimationType.ScaleUp;

        [BoxGroup("$GROUP/网格", ShowLabel = false)]
        [Title("网格")]
        [Tooltip("自动根据 Canvas 尺寸计算网格行列数")]
        [SerializeField] private bool _autoGridSize = true;

        [BoxGroup("$GROUP/网格")]
        [Tooltip("固定列数（autoGridSize=false 时生效）")]
        [HideIf("_autoGridSize")]
        [SerializeField] private int _fixedColumns = 8;

        [BoxGroup("$GROUP/网格")]
        [Tooltip("自动计算时每个方块的目标像素大小")]
        [ShowIf("_autoGridSize")]
        [SerializeField] private EUIBlockSize _targetBlockSize = EUIBlockSize.Size128;

        [BoxGroup("$GROUP/网格")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("当前网格")]
        private string GridRatioDisplay
        {
            get
            {
                RectTransform rect = _containerRect != null ? _containerRect : GetComponent<RectTransform>();
                float cw = rect != null && rect.rect.width > 0f ? rect.rect.width : Screen.width;
                float ch = rect != null && rect.rect.height > 0f ? rect.rect.height : Screen.height;
                Vector2Int grid = ComputeGridSize(cw, ch);
                return $"{grid.x}×{grid.y}（宽高比 {grid.x}:{grid.y}）";
            }
        }

        [BoxGroup("$GROUP/进入动画", ShowLabel = false)]
        [Title("进入动画")]
        [Tooltip("进入排布模式")]
        [DisableIf("IsPresetLocked")]
        [SerializeField] private EUIBlockOrderPattern _enterPattern = EUIBlockOrderPattern.SideWipe;

        [BoxGroup("$GROUP/进入动画")]
        [Tooltip("进入方向/基准角：侧扫=上下左右，螺旋/对角擦除/逐行=四角")]
        [ShowIf("ShowDirection")]
        [SerializeField] private EUIBlockDirection _enterDirection = EUIBlockDirection.Top;

        [BoxGroup("$GROUP/进入动画")]
        [Tooltip("菱形扩散方向：勾选=从中心向外扩散，不勾选=从边缘向内收缩")]
        [ShowIf("ShowDiamondParams")]
        [SerializeField] private bool _diamondOutward = true;

        [BoxGroup("$GROUP/进入动画")]
        [Tooltip("螺旋旋转方向：勾选=顺时针，不勾选=逆时针")]
        [ShowIf("ShowSpiralParams")]
        [SerializeField] private bool _spiralClockwise = true;

        [BoxGroup("$GROUP/进入动画")]
        [Tooltip("螺旋扫入方向：勾选=从中心向外，不勾选=从外向内")]
        [ShowIf("ShowSpiralParams")]
        [SerializeField] private bool _spiralCenterOut = false;

        [BoxGroup("$GROUP/进入动画")]
        [Tooltip("逐行扫描方向：勾选=水平行（自上而下），不勾选=垂直列（自左而右）。先奇数行/列、后偶数行/列，形成交错效果。")]
        [ShowIf("ShowLinesParams")]
        [SerializeField] private bool _linesHorizontal = true;

        [BoxGroup("$GROUP/进入动画")]
        [Tooltip("锯齿交错方向：勾选=锯齿线水平排布（逐行），不勾选=垂直排布（逐列）")]
        [ShowIf("ShowTeethParams")]
        [SerializeField] private bool _teethHorizontal = true;

        [BoxGroup("$GROUP/退出动画", ShowLabel = false)]
        [Title("退出动画")]
        [Tooltip("退出排布模式")]
        [DisableIf("IsPresetLocked")]
        [SerializeField] private EUIBlockOrderPattern _exitPattern = EUIBlockOrderPattern.SideWipe;

        [BoxGroup("$GROUP/退出动画")]
        [Tooltip("勾选后退出动画与进入动画同向（顺放）；不勾选则退出为进入的反向（倒放，默认）。")]
        [HideIf("ShouldHideExitForward")]
        [SerializeField] private bool _exitForward = false;

        [BoxGroup("$GROUP/动画参数", ShowLabel = false)]
        [Title("动画参数")]
        [Tooltip("过渡总时长（秒）")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _transitionDuration = 0.8f;

        [BoxGroup("$GROUP/动画参数")]
        [Tooltip("方块组间错开间隔（秒）")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _staggerInterval = 0.03f;

        [BoxGroup("$GROUP/动画参数")]
        [Tooltip("方块缓动曲线")]
        [SerializeField] private Ease _blockEase = Ease.OutQuad;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private readonly List<BlockEntry> _pool = new();
        private int _activeCount;

        private RectTransform _containerRect;
        private Vector2Int _gridSize;
        private float _blockSize;
        private bool _gridDirty = true;

        private OrderCache _enterCache = new();
        private OrderCache _exitCache = new();

        private const string TAG = LogTags.EmberUI;

        /// <summary>退出方向：默认取进入方向的反向（倒放）；勾选「退出顺放」后与进入同向。</summary>
        private EUIBlockDirection ExitDirection => _exitForward ? _enterDirection : Opposite(_enterDirection);

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            _containerRect = GetComponent<RectTransform>();
            if (_containerRect == null)
                _containerRect = gameObject.AddComponent<RectTransform>();

            _containerRect.anchorMin = Vector2.zero;
            _containerRect.anchorMax = Vector2.one;
            _containerRect.offsetMin = Vector2.zero;
            _containerRect.offsetMax = Vector2.zero;

            var blockCanvas = GetComponent<Canvas>();
            if (blockCanvas == null)
            {
                blockCanvas = gameObject.AddComponent<Canvas>();
                blockCanvas.overrideSorting = true;
                blockCanvas.sortingOrder = -1;
            }
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        #endregion

        // --------------------------------------------------------

        #region IEUITransitionEffect 实现

        public bool HasActiveBlocks => _activeCount > 0;

        [HasGC]
        public async UniTask PlayEnterAsync(float duration = -1f)
        {
            var groups = GetOrder(true);
            if (groups == null || groups.Count == 0) return;
            await PlayTransitionAsync(groups, true, duration);
        }

        [HasGC]
        public async UniTask PlayExitAsync(float duration = -1f)
        {
            var groups = GetOrder(false);
            if (groups == null || groups.Count == 0) return;
            await PlayTransitionAsync(groups, false, duration);
        }

        [NoGC]
        public void HideAllImmediate()
        {
            KillAllTweens();
            for (int i = 0; i < _activeCount; i++)
            {
                var entry = _pool[i];
                if (entry.Rect == null || entry.Raw == null) continue;
                entry.Raw.gameObject.SetActive(false);
                entry.Rect.localScale = Vector3.one;
                var c = entry.Raw.color; c.a = 1f; entry.Raw.color = c;
            }
            _activeCount = 0;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 渐入：方块扫入覆盖屏幕，动画结束后回调 <paramref name="onComplete"/>。
        /// 是 <see cref="PlayEnterAsync"/> 的回调封装，供外部在无需 await 时直接调用。
        /// </summary>
        /// <param name="onComplete">动画结束后的回调，可为 null。</param>
        [HasGC]
        public void FadeIn(Action onComplete = null)
        {
            PlayEnterWithCallback(onComplete).Forget();
        }

        /// <summary>
        /// 渐出：方块移出揭示内容，动画结束后回调 <paramref name="onComplete"/>。
        /// 是 <see cref="PlayExitAsync"/> 的回调封装，供外部在无需 await 时直接调用。
        /// </summary>
        /// <param name="onComplete">动画结束后的回调，可为 null。</param>
        [HasGC]
        public void FadeOut(Action onComplete = null)
        {
            PlayExitWithCallback(onComplete).Forget();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        [HasGC]
        private async UniTask PlayEnterWithCallback(Action onComplete)
        {
            await PlayEnterAsync();
            onComplete?.Invoke();
        }

        [HasGC]
        private async UniTask PlayExitWithCallback(Action onComplete)
        {
            await PlayExitAsync();
            onComplete?.Invoke();
        }

        [HasGC]
        private async UniTask PlayTransitionAsync(List<List<Vector2Int>> groups, bool isEnter, float duration)
        {
            EnsureGrid();

            float dur = duration > 0f ? duration : _transitionDuration;

            int totalNeeded = 0;
            foreach (var g in groups) totalNeeded += g.Count;
            EnsurePoolSize(totalNeeded);

            int groupCount = groups.Count;
            float perBlockDuration = Mathf.Max(0.05f, dur - _staggerInterval * (groupCount - 1));

            KillAllTweens();
            _activeCount = 0;

            for (int gi = 0; gi < groupCount; gi++)
            {
                var group = groups[gi];
                float groupDelay = gi * _staggerInterval;

                for (int bi = 0; bi < group.Count; bi++)
                {
                    var gridPos = group[bi];
                    var entry = _pool[_activeCount++];
                    entry.Raw.gameObject.SetActive(true);
                    ApplyBlockVisual(entry, gridPos.x, gridPos.y);

                    Vector2 targetPos = GetBlockAnchoredPosition(gridPos.x, gridPos.y);
                    entry.Rect.anchoredPosition = targetPos;
                    entry.Rect.sizeDelta = new Vector2(_blockSize, _blockSize);

                    if (isEnter)
                        AnimateBlockEnter(entry, targetPos, groupDelay, perBlockDuration);
                    else
                        AnimateBlockExit(entry, targetPos, groupDelay, perBlockDuration);
                }
            }

            float totalDuration = _staggerInterval * (groupCount - 1) + perBlockDuration;
            await UniTask.Delay(TimeSpan.FromSeconds(totalDuration), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        private void AnimateBlockEnter(BlockEntry entry, Vector2 targetPos, float delay, float duration, bool manualUpdate = false)
        {
            AnimateBlock(entry, targetPos, delay, duration, manualUpdate, isEnter: true);
        }

        private void AnimateBlockExit(BlockEntry entry, Vector2 fromPos, float delay, float duration, bool manualUpdate = false)
        {
            AnimateBlock(entry, fromPos, delay, duration, manualUpdate, isEnter: false);
        }

        private void AnimateBlock(BlockEntry entry, Vector2 position, float delay, float duration, bool manualUpdate, bool isEnter)
        {
            var anim = EUIBlockAnimationRegistry.Get(_blockAnimation);
            if (anim == null) return;

            var ctx = new EUIBlockAnimationContext
            {
                Rect = entry.Rect,
                Raw = entry.Raw,
                Position = position,
                Delay = delay,
                Duration = duration,
                Ease = _blockEase,
                ManualUpdate = manualUpdate,
                SlideOffset = GetSlideOffset(_enterDirection),
            };

            if (isEnter)
                anim.PlayEnter(ctx);
            else
                anim.PlayExit(ctx);
        }

        [NoGC]
        private static EUIBlockDirection Opposite(EUIBlockDirection direction)
        {
            return direction switch
            {
                EUIBlockDirection.Top => EUIBlockDirection.Bottom,
                EUIBlockDirection.Bottom => EUIBlockDirection.Top,
                EUIBlockDirection.Left => EUIBlockDirection.Right,
                EUIBlockDirection.Right => EUIBlockDirection.Left,
                EUIBlockDirection.TopLeft => EUIBlockDirection.BottomRight,
                EUIBlockDirection.TopRight => EUIBlockDirection.BottomLeft,
                EUIBlockDirection.BottomLeft => EUIBlockDirection.TopRight,
                EUIBlockDirection.BottomRight => EUIBlockDirection.TopLeft,
                _ => direction, // Random 等无确定反向，原样返回
            };
        }

        [NoGC]
        private Vector2 GetSlideOffset(EUIBlockDirection direction)
        {
            float w = _containerRect.rect.width, h = _containerRect.rect.height, pad = _blockSize * 1.5f;
            return direction switch
            {
                EUIBlockDirection.Top => new Vector2(0f, h + pad),
                EUIBlockDirection.Bottom => new Vector2(0f, -(h + pad)),
                EUIBlockDirection.Left => new Vector2(-(w + pad), 0f),
                EUIBlockDirection.Right => new Vector2(w + pad, 0f),
                EUIBlockDirection.TopLeft => new Vector2(-(w + pad), h + pad),
                EUIBlockDirection.TopRight => new Vector2(w + pad, h + pad),
                EUIBlockDirection.BottomLeft => new Vector2(-(w + pad), -(h + pad)),
                EUIBlockDirection.BottomRight => new Vector2(w + pad, -(h + pad)),
                _ => new Vector2(0f, h + pad),
            };
        }

        [NoGC]
        private void EnsureGrid()
        {
            if (!_gridDirty) return;
            _gridDirty = false;

            Rect rect = _containerRect.rect;
            float cw = rect.width > 0f ? rect.width : Screen.width;
            float ch = rect.height > 0f ? rect.height : Screen.height;

            _gridSize = ComputeGridSize(cw, ch);

            // 取 Max 而非 Min：让方块取长边那侧的整格，短边那侧略微溢出屏幕，
            // 保证网格始终盖满全屏（否则 Min 会在宽高比不整除时留下边缘缺口）。
            _blockSize = Mathf.Max(cw / _gridSize.x, ch / _gridSize.y);
            _enterCache.Clear();
            _exitCache.Clear();
        }

        /// <summary>
        /// 按当前屏幕尺寸与网格参数计算行列数（纯计算，不缓存）。
        /// 供 <see cref="EnsureGrid"/> 与 Inspector 的「当前网格」展示共用。
        /// </summary>
        [NoGC]
        private Vector2Int ComputeGridSize(float cw, float ch)
        {
            int cols, rows;
            if (_autoGridSize)
            {
                int target = (int)_targetBlockSize;
                cols = Mathf.CeilToInt(cw / target);
                rows = Mathf.CeilToInt(ch / target);
            }
            else
            {
                cols = _fixedColumns;
                rows = Mathf.Max(1, Mathf.CeilToInt(_fixedColumns / (cw / Mathf.Max(ch, 1f))));
            }
            if (cols % 2 == 0) cols++;
            if (rows % 2 == 0) rows++;
            return new Vector2Int(cols, rows);
        }

        [NoGC]
        private Vector2 GetBlockAnchoredPosition(int col, int row)
        {
            float hw = _gridSize.x * _blockSize / 2f, hh = _gridSize.y * _blockSize / 2f;
            return new Vector2(col * _blockSize + _blockSize / 2f - hw, row * _blockSize + _blockSize / 2f - hh);
        }

        /// <summary>
        /// 设置方块的显示内容：有贴图时按网格切出该格的 UV 子区域（拉伸填满），否则用纯色。
        /// </summary>
        [NoGC]
        private void ApplyBlockVisual(BlockEntry entry, int col, int row)
        {
            if (_useBlockTexture && _blockTexture != null)
            {
                entry.Raw.texture = _blockTexture;
                entry.Raw.color = Color.white;
                entry.Raw.uvRect = GetBlockUvRect(col, row);
            }
            else
            {
                entry.Raw.texture = Texture2D.whiteTexture;
                entry.Raw.color = _blockColor;
                entry.Raw.uvRect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        [NoGC]
        private Rect GetBlockUvRect(int col, int row)
        {
            float u = (float)col / _gridSize.x;
            float v = (float)row / _gridSize.y;
            float w = 1f / _gridSize.x;
            float h = 1f / _gridSize.y;
            return new Rect(u, v, w, h);
        }

        [HasGC]
        private List<List<Vector2Int>> GetOrder(bool isEnter)
        {
            EnsureGrid();
            var cache = isEnter ? _enterCache : _exitCache;
            var pattern = isEnter ? _enterPattern : _exitPattern;

            // 退出倒放：菱形/螺旋的「内外」与「倒放」都归结为是否反转基础序列；
            // 其余排布模式的退出方向沿用「对角反向」惯例。
            bool isReversed = !isEnter && !_exitForward;
            var direction = isEnter || pattern == EUIBlockOrderPattern.Spiral
                ? _enterDirection
                : ExitDirection;

            // 螺旋「从中心到外」或「退出倒放」= 反转基础螺旋序列（每组仅 1 块，反转外层组即可）。
            bool reverseOrder = pattern == EUIBlockOrderPattern.Spiral && (_spiralCenterOut ^ isReversed);

            if (cache.Order != null && cache.Pattern == pattern && cache.Direction == direction && cache.Reverse == reverseOrder)
                return cache.Order;

            var orderConfig = new EUIBlockOrderConfig
            {
                Direction = direction,
                BlocksPerGroup = 3,
                DiamondMode = (_diamondOutward ^ isReversed) ? EUIDiamondMode.Outward : EUIDiamondMode.Inward,
                SpiralMode = _spiralClockwise ? EUISpiralMode.Clockwise : EUISpiralMode.CounterClockwise,
                SpiralCorner = direction,
                LinesHorizontal = _linesHorizontal,
                TeethHorizontal = _teethHorizontal,
            };

            var order = EUIBlockOrderCalculator.Calculate(_gridSize.x, _gridSize.y, pattern, orderConfig);

            if (reverseOrder)
                order.Reverse();

            cache.Order = order;
            cache.Pattern = pattern;
            cache.Direction = direction;
            cache.Reverse = reverseOrder;
            return cache.Order;
        }

        [HasGC]
        private void EnsurePoolSize(int required)
        {
            // 清理失效引用：脚本重编译、进出播放模式等场景可能让池里的子物体被销毁，
            // 但非序列化的 _pool 仍残留旧引用（尤其是关闭「Enter Play Mode Options/Reload Domain」时）。
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (_pool[i].Rect == null || _pool[i].Raw == null)
                    _pool.RemoveAt(i);
            }

            while (_pool.Count < required)
            {
                var go = new GameObject("Block", typeof(RectTransform), typeof(RawImage));
                go.hideFlags = HideFlags.DontSave; // 方块是运行时临时物体，不序列化进场景，避免重编译/存场景残留
                go.transform.SetParent(transform, worldPositionStays: false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var raw = go.GetComponent<RawImage>();
                raw.raycastTarget = false;
                go.SetActive(false);
                _pool.Add(new BlockEntry { Rect = rect, Raw = raw });
            }
        }

        [NoGC]
        private void KillAllTweens()
        {
            DOTween.Kill(transform);
            // 方块 tween 的目标是子物体上的 RectTransform / Image，需逐个清理，否则 DOTween.Kill(transform) 杀不到。
            for (int i = 0; i < _pool.Count; i++)
            {
                var entry = _pool[i];
                DOTween.Kill(entry.Rect);
                DOTween.Kill(entry.Raw);
            }
        }

#if UNITY_EDITOR
        [PropertyOrder(90)]
        [BoxGroup("$GROUP/预览", ShowLabel = false)]
        [Title("编辑器预览", "无需进入播放模式。点击「初始化方块池」后再点击预览按钮即可实时预览。")]
        [Button("初始化方块池", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 0.5f)]
        private void EditorInitialize()
        {
            CleanupOrphanBlocks();

            if (_containerRect == null)
            {
                _containerRect = GetComponent<RectTransform>();
                if (_containerRect == null)
                    _containerRect = gameObject.AddComponent<RectTransform>();
                _containerRect.anchorMin = Vector2.zero;
                _containerRect.anchorMax = Vector2.one;
                _containerRect.offsetMin = Vector2.zero;
                _containerRect.offsetMax = Vector2.zero;
            }

            var blockCanvas = GetComponent<Canvas>();
            if (blockCanvas == null)
            {
                blockCanvas = gameObject.AddComponent<Canvas>();
                blockCanvas.overrideSorting = true;
                blockCanvas.sortingOrder = -1;
            }

            EnsureGrid();
            EmberDebug.Log(LogTags.EmberUI, $"初始化完成：{_gridSize.x}×{_gridSize.y} 网格（方块按需创建）");
        }

        /// <summary>
        /// 销毁挂在本节点下、但不在对象池中的孤儿「Block」子物体。
        /// 用于清理旧版本或脚本重编译前遗留的方块，避免层级残留。
        /// </summary>
        private void CleanupOrphanBlocks()
        {
            // 收集池中仍然有效的方块实例
            var pooled = new HashSet<GameObject>();
            for (int i = 0; i < _pool.Count; i++)
            {
                var entry = _pool[i];
                if (entry.Rect != null)
                    pooled.Add(entry.Rect.gameObject);
            }

            int removed = 0;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "Block" && !pooled.Contains(child.gameObject))
                {
                    DestroyImmediate(child.gameObject);
                    removed++;
                }
            }

            if (removed > 0)
                EmberDebug.Log(LogTags.EmberUI, $"清理孤儿方块 {removed} 个");
        }

        [PropertyOrder(91)]
        [BoxGroup("$GROUP/预览")]
        [HorizontalGroup("$GROUP/预览/行1")]
        [Button("▶ 预览进入", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 0.3f)]
        private void EditorPreviewEnter()
        {
            DOTween.Init(true, true, LogBehaviour.Default);
            EditorInitialize();
            EditorPlayEnterPreview();
        }

        [PropertyOrder(92)]
        [BoxGroup("$GROUP/预览")]
        [HorizontalGroup("$GROUP/预览/行1")]
        [Button("▶ 预览退出", ButtonSizes.Large), GUIColor(0.7f, 0.5f, 0.2f)]
        private void EditorPreviewExit()
        {
            DOTween.Init(true, true, LogBehaviour.Default);
            EditorInitialize();
            EditorPlayExitPreview();
        }

        [PropertyOrder(93)]
        [BoxGroup("$GROUP/预览")]
        [Button("■ 停止", ButtonSizes.Medium), GUIColor(0.7f, 0.3f, 0.3f)]
        private void EditorPreviewStop()
        {
            KillAllTweens();
            DestroyPooledBlocks();
        }

        /// <summary>
        /// 销毁对象池里所有实例化出来的方块并清空池。编辑器「停止」时清理，
        /// 防止污染预制体（运行时播放会自行重新初始化池）。
        /// </summary>
        private void DestroyPooledBlocks()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var entry = _pool[i];
                if (entry.Rect != null)
                    DestroyImmediate(entry.Rect.gameObject);
            }
            _pool.Clear();
            _activeCount = 0;
        }

        [BoxGroup("$GROUP/动画参数")]
        [Button("重置动画参数", ButtonSizes.Medium), GUIColor(0.9f, 0.7f, 0.4f)]
        private void ResetAnimationParameters()
        {
            ApplyDefaultAnimationParameters();
        }

        private void OnValidate()
        {
            _gridDirty = true;
            _enterCache.Clear();
            _exitCache.Clear();
        }

        private static readonly ValueDropdownList<EUIBlockPreset> PresetItems = new()
        {
            { "自定义", EUIBlockPreset.Custom },
            { "▸ 侧扫", EUIBlockPreset.SideWipe },
            { "◇ 菱形", EUIBlockPreset.Diamond },
            { "◎ 螺旋", EUIBlockPreset.Spiral },
            { "▦ 棋盘格交错", EUIBlockPreset.Checkerboard },
            { "◤ 对角擦除", EUIBlockPreset.CornerWipe },
            { "★ 随机飞入", EUIBlockPreset.Random },
            { "≡ 逐行扫描", EUIBlockPreset.Lines },
            { "⫶ 锯齿交错", EUIBlockPreset.Teeth },
        };

        /// <summary>非「自定义」预设时锁定排布模式与动画类型字段，方向/基准角与子参数仍可手动调整。</summary>
        private bool IsPresetLocked() => _preset != EUIBlockPreset.Custom;

        /// <summary>进入方向/基准角是否展示：侧扫、螺旋、对角擦除、逐行需要方向/基准角。</summary>
        private bool ShowDirection() => _enterPattern is EUIBlockOrderPattern.SideWipe
            or EUIBlockOrderPattern.Spiral
            or EUIBlockOrderPattern.CornerWipe
            or EUIBlockOrderPattern.Lines;

        /// <summary>菱形子参数是否展示。</summary>
        private bool ShowDiamondParams() => _enterPattern == EUIBlockOrderPattern.Diamond;

        /// <summary>螺旋子参数是否展示。</summary>
        private bool ShowSpiralParams() => _enterPattern == EUIBlockOrderPattern.Spiral;

        /// <summary>逐行扫描子参数是否展示。</summary>
        private bool ShowLinesParams() => _enterPattern == EUIBlockOrderPattern.Lines;

        /// <summary>锯齿交错子参数是否展示。</summary>
        private bool ShowTeethParams() => _enterPattern == EUIBlockOrderPattern.Teeth;

        /// <summary>
        /// 「退出顺放」开关的显隐条件：进入方向为 Random（无确定反向），
        /// 或退出排布模式本身不看方向时，隐藏该开关。
        /// </summary>
        private bool ShouldHideExitForward()
        {
            if (_enterDirection == EUIBlockDirection.Random) return true;
            return _exitPattern == EUIBlockOrderPattern.Checkerboard
                || _exitPattern == EUIBlockOrderPattern.AllAtOnce
                || _exitPattern == EUIBlockOrderPattern.Random;
        }

        private ValueDropdownList<EUIBlockPreset> GetPresetItems() => PresetItems;

        /// <summary>
        /// 应用当前预设的默认动画参数。螺旋等「每块一组」的排布会因错开间隔累加导致总时长过长，
        /// 因此这类预设用更小的默认错开间隔。
        /// </summary>
        private void ApplyDefaultAnimationParameters()
        {
            _transitionDuration = 0.8f;
            _blockEase = Ease.OutQuad;
            _staggerInterval = EUIBlockPresetDefaults.GetStagger(_preset);
        }

        private void ApplyPreset()
        {
            if (_preset == EUIBlockPreset.Custom) return;

            switch (_preset)
            {
                case EUIBlockPreset.SideWipe:
                    _enterPattern = EUIBlockOrderPattern.SideWipe; _enterDirection = EUIBlockDirection.Top;
                    _exitPattern = EUIBlockOrderPattern.SideWipe;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.Diamond:
                    _enterPattern = EUIBlockOrderPattern.Diamond; _diamondOutward = true;
                    _exitPattern = EUIBlockOrderPattern.Diamond;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.Spiral:
                    _enterPattern = EUIBlockOrderPattern.Spiral; _enterDirection = EUIBlockDirection.TopLeft;
                    _spiralClockwise = true; _spiralCenterOut = false;
                    _exitPattern = EUIBlockOrderPattern.Spiral;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.Checkerboard:
                    _enterPattern = EUIBlockOrderPattern.Checkerboard;
                    _exitPattern = EUIBlockOrderPattern.Checkerboard;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.CornerWipe:
                    _enterPattern = EUIBlockOrderPattern.CornerWipe; _enterDirection = EUIBlockDirection.TopLeft;
                    _exitPattern = EUIBlockOrderPattern.CornerWipe;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.Random:
                    _enterPattern = EUIBlockOrderPattern.Random;
                    _exitPattern = EUIBlockOrderPattern.Random;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.Lines:
                    _enterPattern = EUIBlockOrderPattern.Lines; _enterDirection = EUIBlockDirection.TopLeft; _linesHorizontal = true;
                    _exitPattern = EUIBlockOrderPattern.Lines;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
                case EUIBlockPreset.Teeth:
                    _enterPattern = EUIBlockOrderPattern.Teeth; _teethHorizontal = true;
                    _exitPattern = EUIBlockOrderPattern.Teeth;
                    _blockAnimation = EUIBlockAnimationType.ScaleUp; break;
            }

            ApplyDefaultAnimationParameters();
        }

        public void EditorPlayEnterPreview()
        {
            var groups = GetOrder(true);
            if (groups == null || groups.Count == 0) return;
            EditorPlayPreview(groups, true);
        }

        public void EditorPlayExitPreview()
        {
            var groups = GetOrder(false);
            if (groups == null || groups.Count == 0) return;
            EditorPlayPreview(groups, false);
        }

        [HasGC]
        private void EditorPlayPreview(List<List<Vector2Int>> groups, bool isEnter)
        {
            EnsureGrid();

            int totalNeeded = 0;
            foreach (var g in groups) totalNeeded += g.Count;
            EnsurePoolSize(totalNeeded);

            int groupCount = groups.Count;
            float perBlockDuration = Mathf.Max(0.05f, _transitionDuration - _staggerInterval * (groupCount - 1));

            KillAllTweens();
            _activeCount = 0;

            for (int gi = 0; gi < groupCount; gi++)
            {
                var group = groups[gi];
                float groupDelay = gi * _staggerInterval;
                for (int bi = 0; bi < group.Count; bi++)
                {
                    var gridPos = group[bi];
                    var entry = _pool[_activeCount++];
                    entry.Raw.gameObject.SetActive(true);
                    ApplyBlockVisual(entry, gridPos.x, gridPos.y);
                    Vector2 targetPos = GetBlockAnchoredPosition(gridPos.x, gridPos.y);
                    entry.Rect.anchoredPosition = targetPos;
                    entry.Rect.sizeDelta = new Vector2(_blockSize, _blockSize);

                    if (isEnter)
                        AnimateBlockEnter(entry, targetPos, groupDelay, perBlockDuration, manualUpdate: true);
                    else
                        AnimateBlockExit(entry, targetPos, groupDelay, perBlockDuration, manualUpdate: true);
                }
            }
        }
#endif

        #endregion

        // --------------------------------------------------------

        #region 内部类型

        private sealed class OrderCache
        {
            public List<List<Vector2Int>> Order;
            public EUIBlockOrderPattern Pattern;
            public EUIBlockDirection Direction;
            public bool Reverse;

            [NoGC]
            public void Clear() { Order = null; Pattern = default; Direction = default; Reverse = false; }
        }

        [Serializable]
        private class BlockEntry
        {
            public RectTransform Rect;
            public RawImage Raw;
        }

        #endregion
    }
}
