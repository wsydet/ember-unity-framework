using System;
using System.Globalization;
using Ember.Basic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    public enum EUIProgressWhiteMode { Disabled, Shrink, Fade }
    public enum EUIProgressTextMode { None, Percent, CurrentOverMax, Custom }

    /// <summary>展示专用进度：即时主条、独立慢尾与损失矩形。通过显式 Extension 绑定。
    /// 借入目标由本组件独占；回池登记 lease.Track(bar.Clear)。</summary>
    [DisallowMultipleComponent, AddComponentMenu("UI/EUI/Buffered Progress Bar")]
    [EUIExtension(typeof(EUIBufferedProgressBar), AutoDetect = false)]
    public sealed class EUIBufferedProgressBar : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Slider _mainSlider;
        [SerializeField] private Image _mainImage;
        [SerializeField] private Slider _slowSlider;
        [SerializeField] private Image _slowImage;
        [SerializeField] private Image _whiteImage;
        [SerializeField] private RectTransform _whiteTrack;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private bool _slowBufferEnabled = true;
        [SerializeField] private EUIProgressWhiteMode _whiteMode = EUIProgressWhiteMode.Shrink;
        [SerializeField] private float _slowDelay = .35f;
        [SerializeField] private float _slowDuration = .65f;
        [SerializeField] private float _whiteDelay = .08f;
        [SerializeField] private float _whiteDuration = .22f;
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private EUIProgressTextMode _textMode = EUIProgressTextMode.Percent;
        [SerializeField, Range(0, 4)] private int _textPrecision;
        [SerializeField] private string _customTextFormat = "{0:0} / {1:0}";
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private float _current, _maximum = 1, _normalized, _slowEnd, _whiteEnd, _whiteAlpha;
        private bool _initialized, _valueMode, _mainValid, _slowValid, _whiteValid;
        private Slider.Direction _direction;
        private Image _ownedWhite;
        private Color _whiteColor;
        private Motion _slowMotion, _whiteMotion;
        private string _diagnostic, _textDiagnostic, _lastWarning;

        private struct Motion
        {
            public bool Active;
            public float Start;
            public double Hold, Elapsed;
            public void Begin(float start, float delay)
            { Active = true; Start = start; Hold = delay; Elapsed = 0; }
            public bool Advance(float dt, float duration, out float t)
            {
                double remaining = dt;
                double consumed = Math.Min(Hold, remaining);
                Hold -= consumed; remaining -= consumed;
                if (Hold > 0) { t = 0; return false; }
                Elapsed += remaining;
                t = duration <= 0 ? 1 : (float)Math.Min(1, Elapsed / duration);
                Active = t < 1;
                return true;
            }
        }

        public float CurrentValue => _current;
        public float MaxValue => _maximum;
        public float NormalizedValue => _normalized;
        public bool IsAnimating => _slowMotion.Active || _whiteMotion.Active;
        /// <summary>当前配置诊断；读取不写入任何显示目标。</summary>
        public string Diagnostic => ConfigurationDiagnostic() ?? _textDiagnostic;
        public bool SlowBufferEnabled
        { get => _slowBufferEnabled; set { if (_slowBufferEnabled == value) return; _slowBufferEnabled = value; ResetSlow(); ApplyVisuals(); } }
        public EUIProgressWhiteMode WhiteMode
        { get => _whiteMode; set { value = Enum.IsDefined(typeof(EUIProgressWhiteMode), value) ? value : EUIProgressWhiteMode.Disabled; if (_whiteMode == value) return; _whiteMode = value; ResetWhite(); ApplyVisuals(); } }
        public float SlowDelay { get => _slowDelay; set => SetTiming(ref _slowDelay, value, true); }
        public float SlowDuration { get => _slowDuration; set => SetTiming(ref _slowDuration, value, true); }
        public float WhiteDelay { get => _whiteDelay; set => SetTiming(ref _whiteDelay, value, false); }
        public float WhiteDuration { get => _whiteDuration; set => SetTiming(ref _whiteDuration, value, false); }
        public bool UseUnscaledTime
        { get => _useUnscaledTime; set { if (_useUnscaledTime == value) return; _useUnscaledTime = value; SyncImmediate(); } }
        public EUIProgressTextMode TextMode
        { get => _textMode; set { if (_textMode == value) return; _textMode = value; RefreshText(); } }
        public int TextPrecision
        { get => _textPrecision; set { value = Mathf.Clamp(value, 0, 4); if (_textPrecision == value) return; _textPrecision = value; RefreshText(); } }
        public string CustomTextFormat
        { get => _customTextFormat; set { if (_customTextFormat == value) return; _customTextFormat = value; RefreshText(); } }
        public TMP_Text ProgressText
        { get => _progressText; set { if (_progressText == value) return; _progressText = value; RefreshText(); } }
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake() { EnsureInitialized(); }
        private void OnEnable() { EnsureInitialized(); }
        private void OnDisable() { SyncImmediate(); }
        private void OnDestroy() { _slowMotion = default; _whiteMotion = default; RetireOwnedWhite(); }
        private void LateUpdate()
        {
            if (!IsAnimating) return;
            Tick(_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private float Timing(float value) => Finite(value) ? Mathf.Clamp(value, 0, 3600) : 0;
        private void SetTiming(ref float field, float value, bool slow)
        {
            value = Timing(value);
            if (field == value) return;
            field = value;
            if (slow) ResetSlow(); else ResetWhite();
            ApplyVisuals();
        }
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _slowDelay = Timing(_slowDelay); _slowDuration = Timing(_slowDuration);
            _whiteDelay = Timing(_whiteDelay); _whiteDuration = Timing(_whiteDuration);
            SyncImmediate();
        }
        private void Accept(float current, float maximum, bool valueMode, bool immediate)
        {
            bool first = !_initialized;
            _initialized = true;
            float n = maximum > 0 ? (float)((double)current / maximum) : 0;
            bool changedRange = valueMode != _valueMode || maximum != _maximum;
            if (!first && !changedRange && current == _current)
            { if (immediate) SyncImmediate(); return; }
            float previous = _normalized;
            _current = current; _maximum = maximum; _normalized = n; _valueMode = valueMode;
            ValidateTargets();
            if (first || immediate || changedRange || !isActiveAndEnabled || n > previous || !_mainValid)
            { SyncImmediate(); return; }
            if (n < previous)
            {
                if (_slowBufferEnabled && _slowValid)
                { _slowEnd = Mathf.Max(_slowEnd, previous); _slowMotion.Begin(_slowEnd, Timing(_slowDelay)); }
                else ResetSlow();
                if (_whiteMode != EUIProgressWhiteMode.Disabled && _whiteValid)
                {
                    bool faded = _whiteMode == EUIProgressWhiteMode.Fade && (!_whiteMotion.Active || _whiteMotion.Elapsed > 0);
                    _whiteEnd = faded ? previous : Mathf.Max(_whiteEnd, previous);
                    _whiteAlpha = 1; _whiteMotion.Begin(_whiteEnd, Timing(_whiteDelay));
                }
                else ResetWhite();
            }
            Tick(0); RefreshText();
        }
        private void ResetSlow() { _slowMotion = default; _slowEnd = _normalized; }
        private void ResetWhite()
        { _whiteMotion = default; _whiteEnd = _normalized; _whiteAlpha = 0; RetireOwnedWhite(); }
        private void Tick(float dt)
        {
            dt = Finite(dt) ? Mathf.Max(0, dt) : 0;
            ValidateTargets();
            if (!_mainValid) { ResetSlow(); ResetWhite(); }
            if (!_slowBufferEnabled || !_slowValid) ResetSlow();
            if (_whiteMode == EUIProgressWhiteMode.Disabled || !_whiteValid) ResetWhite();
            if (_slowMotion.Active && _slowMotion.Advance(dt, Timing(_slowDuration), out float st))
                _slowEnd = st == 1 ? _normalized : Mathf.Lerp(_slowMotion.Start, _normalized, st);
            if (_whiteMotion.Active && _whiteMotion.Advance(dt, Timing(_whiteDuration), out float wt))
            {
                if (_whiteMode == EUIProgressWhiteMode.Shrink)
                    _whiteEnd = wt == 1 ? _normalized : Mathf.Lerp(_whiteMotion.Start, _normalized, wt);
                else _whiteAlpha = 1 - wt;
                if (wt == 1) ResetWhite();
            }
            ApplyVisuals();
        }
        private void RetireOwnedWhite()
        {
            // Only a previously validated dedicated image is ever retired.
            if (_ownedWhite && _ownedWhite != Graphic(_mainSlider, _mainImage) && _ownedWhite != Graphic(_slowSlider, _slowImage))
            { _ownedWhite.color = _whiteColor; _ownedWhite.enabled = false; }
        }
        private RectTransform Track(Slider slider, Image image)
            => slider && slider.fillRect ? slider.fillRect.parent as RectTransform : image ? image.rectTransform : null;
        private Graphic Graphic(Slider slider, Image image)
            => slider && slider.fillRect ? slider.fillRect.GetComponent<Graphic>() : image;
        private string TargetIssue(Slider slider, Image image, out Slider.Direction direction)
        {
            direction = Slider.Direction.LeftToRight;
            if (slider && image) return "每层只允许 Slider 或 Image 一个目标。";
            if (slider)
            {
                direction = slider.direction;
                if (!Finite(slider.minValue) || !Finite(slider.maxValue) || slider.minValue >= slider.maxValue)
                    return "Slider 要求有限 min < max。";
                if (slider.wholeNumbers || !slider.fillRect || slider.interactable)
                    return "Slider 要求 wholeNumbers=false、fillRect、interactable=false；不支持交互/外部双写。";
                var fill = slider.fillRect.GetComponent<Image>();
                if (fill && fill.type == Image.Type.Filled)
                {
                    // Native Slider only writes fillAmount for Filled images; it does not
                    // synchronize their method/origin with its own direction.
                    string issue = TargetIssue(null, fill, out var fillDirection);
                    if (issue != null) return "Slider fillRect：" + issue;
                    if (fillDirection != direction)
                        return "Slider fillRect 的 Filled Image 方向必须与 Slider.direction 一致。";
                }
                return null;
            }
            if (!image) return "缺少显示目标。";
            if (image.type != Image.Type.Filled || image.preserveAspect || image.fillOrigin < 0 || image.fillOrigin > 1)
                return "Image 要求 Filled Horizontal/Vertical、origin 0/1、preserveAspect=false；不支持 radial。";
            if (image.fillMethod == Image.FillMethod.Horizontal)
                direction = image.fillOrigin == 0 ? Slider.Direction.LeftToRight : Slider.Direction.RightToLeft;
            else if (image.fillMethod == Image.FillMethod.Vertical)
                direction = image.fillOrigin == 0 ? Slider.Direction.BottomToTop : Slider.Direction.TopToBottom;
            else return "Image 不支持 radial；仅 Horizontal/Vertical。";
            return null;
        }
        private bool SlowConflict()
        {
            var mainGraphic = Graphic(_mainSlider, _mainImage);
            var slowGraphic = Graphic(_slowSlider, _slowImage);
            return (_mainSlider && _mainSlider == _slowSlider) ||
                (_mainSlider && _slowSlider && _mainSlider.fillRect && _mainSlider.fillRect == _slowSlider.fillRect) ||
                (mainGraphic && mainGraphic == slowGraphic);
        }
        private bool Aligned(RectTransform a, RectTransform b)
        {
            if (!a || !b) return false;
            // Compare authored full tracks in world space, including orientation and scale.
            var ar = a.rect; var br = b.rect;
            return (a.TransformPoint(ar.min) - b.TransformPoint(br.min)).sqrMagnitude < .01f &&
                (a.TransformPoint(ar.max) - b.TransformPoint(br.max)).sqrMagnitude < .01f &&
                (a.TransformPoint(new Vector2(ar.xMin, ar.yMax)) - b.TransformPoint(new Vector2(br.xMin, br.yMax))).sqrMagnitude < .01f;
        }
        private string WhiteIssue()
        {
            if (!_whiteImage || !_whiteTrack) return "白条需要专用 Image 和完整 White Track。";
            var rect = _whiteImage.rectTransform;
            if (rect == _whiteTrack || rect.parent != _whiteTrack ||
                _whiteTrack.IsChildOf(rect) || _whiteImage == Graphic(_mainSlider, _mainImage) ||
                _whiteImage == Graphic(_slowSlider, _slowImage) ||
                (_mainSlider && rect == _mainSlider.fillRect) || (_slowSlider && rect == _slowSlider.fillRect) ||
                Track(_mainSlider, _mainImage) == rect || Track(_slowSlider, _slowImage) == rect ||
                (Track(_mainSlider, _mainImage) && Track(_mainSlider, _mainImage).IsChildOf(rect)) ||
                (Track(_slowSlider, _slowImage) && Track(_slowSlider, _slowImage).IsChildOf(rect)) ||
                (_mainSlider && _mainSlider.fillRect && _whiteTrack.IsChildOf(_mainSlider.fillRect)) ||
                (_slowSlider && _slowSlider.fillRect && _whiteTrack.IsChildOf(_slowSlider.fillRect)))
                return "白条引用冲突：必须为独立轨道的直接子 Image，不能复用主/慢 Graphic、fillRect 或轨道。";
            if ((_whiteImage.type != Image.Type.Simple && _whiteImage.type != Image.Type.Sliced) ||
                _whiteImage.preserveAspect || _whiteImage.raycastTarget)
                return "白条要求 Simple/Sliced、preserveAspect=false、raycastTarget=false。";
            if (!Aligned(Track(_mainSlider, _mainImage), _whiteTrack)) return "白条完整轨道必须与主条同向对齐。";
            if (_whiteTrack.GetComponent<LayoutGroup>() || rect.GetComponent<ContentSizeFitter>() ||
                rect.GetComponent<AspectRatioFitter>() || rect.GetComponent<Animator>())
                return "白条矩形不能由 Layout/Animator 竞争驱动。";
            return null;
        }
        private string ConfigurationDiagnostic()
        {
            string main = TargetIssue(_mainSlider, _mainImage, out var direction);
            if (main != null) return "主条：" + main;
            if (_slowBufferEnabled)
            {
                string slow = TargetIssue(_slowSlider, _slowImage, out var slowDirection);
                if (slow != null) return "慢尾：" + slow;
                if (SlowConflict()) return "慢尾与主条共享目标/fillRect/Graphic，已停止慢尾写入。";
                if (direction != slowDirection) return "慢尾方向必须与主条一致。";
                if (!Aligned(Track(_mainSlider, _mainImage), Track(_slowSlider, _slowImage))) return "慢尾完整轨道必须与主条同向对齐。";
            }
            if (_whiteMode != EUIProgressWhiteMode.Disabled) return WhiteIssue();
            return null;
        }
        private void ValidateTargets()
        {
            _mainValid = TargetIssue(_mainSlider, _mainImage, out _direction) == null;
            _slowValid = _mainValid && TargetIssue(_slowSlider, _slowImage, out var sd) == null && sd == _direction && !SlowConflict() &&
                Aligned(Track(_mainSlider, _mainImage), Track(_slowSlider, _slowImage));
            _whiteValid = _mainValid && WhiteIssue() == null;
            if (_whiteValid && _ownedWhite != _whiteImage)
            { RetireOwnedWhite(); _ownedWhite = _whiteImage; _whiteColor = _whiteImage.color; }
            // A reference that has become an alias must not be touched even during cleanup.
            if (!_whiteValid) { RetireOwnedWhite(); _ownedWhite = null; }
            _diagnostic = ConfigurationDiagnostic();
            Warn(_diagnostic ?? _textDiagnostic);
        }
        private void Warn(string message)
        {
            if (message == _lastWarning) return;
            _lastWarning = message;
            if (message != null) EmberDebug.LogWarning(LogTags.UIManager, "EUIBufferedProgressBar: " + message);
        }
        private void WriteTarget(Slider slider, Image image, float n)
        {
            if (slider)
            {
                // Double avoids overflow in max-min even when both endpoints are finite floats.
                float value = (float)((double)slider.minValue * (1 - (double)n) + (double)slider.maxValue * n);
                if (Finite(value)) slider.SetValueWithoutNotify(value);
                // Native Slider uses float InverseLerp. Extreme finite ranges can overflow
                // there; repair its display anchors from our finite normalized data.
                if (!Finite(slider.maxValue - slider.minValue))
                {
                    int axis = (int)slider.direction > 1 ? 1 : 0;
                    bool reverse = slider.direction == Slider.Direction.RightToLeft || slider.direction == Slider.Direction.TopToBottom;
                    var min = Vector2.zero; var max = Vector2.one;
                    var fill = slider.fillRect.GetComponent<Image>();
                    if (fill && fill.type == Image.Type.Filled) fill.fillAmount = n;
                    else if (reverse) min[axis] = 1 - n; else max[axis] = n;
                    slider.fillRect.anchorMin = min; slider.fillRect.anchorMax = max;
                    if (slider.handleRect)
                    { min = Vector2.zero; max = Vector2.one; min[axis] = max[axis] = reverse ? 1 - n : n; slider.handleRect.anchorMin = min; slider.handleRect.anchorMax = max; }
                }
            }
            else if (image) image.fillAmount = n;
        }
        private void ApplyVisuals()
        {
            ValidateTargets();
            if (_mainValid) WriteTarget(_mainSlider, _mainImage, _normalized);
            if (_slowValid) WriteTarget(_slowSlider, _slowImage, _slowBufferEnabled ? _slowEnd : _normalized);
            if (!_whiteValid) return;
            var r = _whiteImage.rectTransform;
            float low = _normalized, high = Mathf.Clamp(_whiteEnd, low, 1);
            bool vertical = _direction == Slider.Direction.BottomToTop || _direction == Slider.Direction.TopToBottom;
            bool reverse = _direction == Slider.Direction.RightToLeft || _direction == Slider.Direction.TopToBottom;
            if (reverse) { float oldLow = low; low = 1 - high; high = 1 - oldLow; }
            r.anchorMin = vertical ? new Vector2(0, low) : new Vector2(low, 0);
            r.anchorMax = vertical ? new Vector2(1, high) : new Vector2(high, 1);
            r.offsetMin = r.offsetMax = Vector2.zero;
            bool visible = _whiteMode != EUIProgressWhiteMode.Disabled && _whiteAlpha > 0 && high > low;
            _whiteImage.color = visible ? new Color(_whiteColor.r, _whiteColor.g, _whiteColor.b, _whiteColor.a * _whiteAlpha) : _whiteColor;
            _whiteImage.enabled = visible;
        }
        private string Number(double value) => Math.Round(value, Mathf.Clamp(_textPrecision, 0, 4), MidpointRounding.AwayFromZero)
            .ToString("F" + Mathf.Clamp(_textPrecision, 0, 4), CultureInfo.InvariantCulture);
        private string Percent() => Number((double)_normalized * 100) + "%";
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>进入 0..1 模式；首次、inactive、模式变化或 immediate 输入立即同步。</summary>
        public void SetNormalized(float value, bool immediate = false)
            => Accept(Finite(value) ? Mathf.Clamp01(value) : 0, 1, false, immediate);
        /// <summary>非有限/非正上限输出 0/0；current 非有限按 0，有限值钳制到范围。</summary>
        public void SetValue(float current, float maximum, bool immediate = false)
        {
            if (!Finite(maximum) || maximum <= 0) { current = maximum = 0; }
            else current = Finite(current) ? Mathf.Clamp(current, 0, maximum) : 0;
            Accept(current, maximum, true, immediate);
        }
        public void SyncImmediate()
        { ResetSlow(); ResetWhite(); ApplyVisuals(); RefreshText(); }
        public void Clear()
        { _initialized = true; _valueMode = false; _current = _normalized = 0; _maximum = 1; SyncImmediate(); }
        /// <summary>借入目标，不创建/销毁资源。重绑清退旧白条并同步；文字可为空。</summary>
        public void ConfigureTargets(Slider mainSlider, Image mainImage, Slider slowSlider, Image slowImage,
            Image whiteImage, RectTransform whiteTrack, TMP_Text progressText)
        {
            RetireOwnedWhite(); _ownedWhite = null;
            _mainSlider = mainSlider; _mainImage = mainImage; _slowSlider = slowSlider; _slowImage = slowImage;
            _whiteImage = whiteImage; _whiteTrack = whiteTrack; _progressText = progressText;
            SyncImmediate();
        }
        /// <summary>仅更新本实例文字。TMPEx Key 非空时保留其本地化所有权。</summary>
        public void RefreshText()
        {
            _textDiagnostic = null;
            if (!_progressText || _textMode == EUIProgressTextMode.None) { Warn(_diagnostic); return; }
            if (_progressText is TMPEx keyed && !string.IsNullOrEmpty(keyed.Key))
            { _textDiagnostic = "数字 TMPEx 的 Key 必须为空；已跳过文字写入。"; Warn(_diagnostic ?? _textDiagnostic); return; }
            string text;
            if (_textMode == EUIProgressTextMode.CurrentOverMax) text = Number(_current) + " / " + Number(_maximum);
            else if (_textMode == EUIProgressTextMode.Custom)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_customTextFormat)) throw new FormatException();
                    text = string.Format(CultureInfo.InvariantCulture, _customTextFormat, _current, _maximum, _normalized, (double)_normalized * 100);
                }
                catch (FormatException)
                { text = Percent(); _textDiagnostic = "自定义格式无效，已回退百分比。"; }
            }
            else text = Percent();
            if (_progressText is TMPEx ex) ex.SetSource(text); else _progressText.text = text;
            Warn(_diagnostic ?? _textDiagnostic);
        }
        #endregion
    }
}
