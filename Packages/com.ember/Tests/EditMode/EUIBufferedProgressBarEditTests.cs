using System;
using System.Collections.Generic;
using System.Reflection;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ember.UI.Tests
{
    public sealed class EUIBufferedProgressBarEditTests
    {
        private GameObject _root;
        private Image _main, _slow, _white;
        private RectTransform _track;
        private EUIBufferedProgressBar _bar;
        private TMP_FontAsset _font;
        private Sprite _sprite;
        private Texture2D _texture;
        private readonly Color _baseColor = new Color(.9f, .8f, .7f, .75f);
        private readonly MethodInfo _tick = typeof(EUIBufferedProgressBar).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp] public void SetUp()
        {
            _root = new GameObject("BufferedProgressFixture", typeof(RectTransform));
            _track = Rect("Track", _root.transform);
            _slow = Fill("Slow"); _white = Rect("White", _track).gameObject.AddComponent<Image>();
            _white.raycastTarget = false; _white.color = _baseColor;
            _main = Fill("Main"); _bar = _root.AddComponent<EUIBufferedProgressBar>();
            _bar.ConfigureTargets(null, _main, null, _slow, _white, _track, null);
            _bar.SetValue(100, 100);
        }
        private RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(400, 30); return rect;
        }
        private Image Fill(string name)
        { var image = Rect(name, _track).gameObject.AddComponent<Image>(); image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; return image; }
        private void Tick(float dt) => _tick.Invoke(_bar, new object[] { dt });
        private Slider SpriteSlider(Slider.Direction direction, Image.Type type, out Image fill)
        {
            if (!_sprite)
            {
                _texture = new Texture2D(8, 8);
                var pixels = new Color[64]; for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                _texture.SetPixels(pixels); _texture.Apply();
                _sprite = Sprite.Create(_texture, new Rect(0, 0, 8, 8), Vector2.one * .5f, 100, 0,
                    SpriteMeshType.FullRect, Vector4.one * 2);
            }
            var slider = Rect("SpriteSlider", _track).gameObject.AddComponent<Slider>();
            fill = Rect("SpriteFill", slider.transform).gameObject.AddComponent<Image>();
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.sprite = _sprite; fill.type = type;
            fill.fillMethod = (int)direction > 1 ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            fill.fillOrigin = direction == Slider.Direction.RightToLeft || direction == Slider.Direction.TopToBottom ? 1 : 0;
            slider.interactable = false; slider.minValue = -50; slider.maxValue = 150;
            slider.fillRect = fill.rectTransform; slider.direction = direction;
            return slider;
        }
        private Rect MeshBoundsInTrack(Image image)
        {
            using (var helper = new VertexHelper())
            {
                typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(image, new object[] { helper });
                Assert.Greater(helper.currentVertCount, 0, "Need actual sprite vertices, not only Slider.value/anchors.");
                Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                var vertex = new UIVertex();
                for (int i = 0; i < helper.currentVertCount; i++)
                {
                    helper.PopulateUIVertex(ref vertex, i);
                    Vector2 p = _track.InverseTransformPoint(image.rectTransform.TransformPoint(vertex.position));
                    min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                }
                return UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }
        private TMPEx Text()
        {
            _font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi.ttf"));
            _font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            Assert.IsTrue(_font.TryAddCharacters("0123456789% /.abc"));
            _font.atlasPopulationMode = AtlasPopulationMode.Static;
            var rect = Rect("Text", _root.transform); rect.gameObject.SetActive(false);
            var text = rect.gameObject.AddComponent<TMPEx>(); text.SetFontSkin(-1, 1);
            text.font = _font; text.fontSharedMaterial = _font.material; text.Key = "";
            _bar.ProgressText = text; rect.gameObject.SetActive(true); return text;
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(_root);
            if (_sprite) Object.DestroyImmediate(_sprite);
            if (_texture) Object.DestroyImmediate(_texture);
            if (_font)
            {
                var material = _font.material; var textures = _font.atlasTextures;
                Object.DestroyImmediate(_font);
                if (material) Object.DestroyImmediate(material);
                foreach (var texture in textures) if (texture) Object.DestroyImmediate(texture);
            }
        }

        [TestCase(false, EUIProgressWhiteMode.Disabled)] [TestCase(true, EUIProgressWhiteMode.Disabled)]
        [TestCase(false, EUIProgressWhiteMode.Shrink)] [TestCase(false, EUIProgressWhiteMode.Fade)]
        [TestCase(true, EUIProgressWhiteMode.Shrink)] [TestCase(true, EUIProgressWhiteMode.Fade)]
        public void SixCombinations_UseActualLossRectangleAndIndependentClocks(bool slow, EUIProgressWhiteMode white)
        {
            _bar.SlowBufferEnabled = slow; _bar.WhiteMode = white; _bar.SetValue(60, 100);
            Assert.AreEqual(.6f, _main.fillAmount); Assert.AreEqual(slow ? 1 : .6f, _slow.fillAmount);
            Assert.AreEqual(white != EUIProgressWhiteMode.Disabled, _white.enabled);
            if (_white.enabled) { Assert.AreEqual(.6f, _white.rectTransform.anchorMin.x); Assert.AreEqual(1, _white.rectTransform.anchorMax.x); }
            Tick(.19f);
            if (white == EUIProgressWhiteMode.Shrink) Assert.That(_white.rectTransform.anchorMax.x, Is.EqualTo(.8f).Within(.001));
            if (white == EUIProgressWhiteMode.Fade) { Assert.AreEqual(1, _white.rectTransform.anchorMax.x); Assert.That(_white.color.a, Is.EqualTo(.375f).Within(.001)); }
            if (slow) Assert.AreEqual(1, _slow.fillAmount);
            Tick(.12f); Assert.IsFalse(_white.enabled); Assert.AreEqual(_baseColor, _white.color);
            Tick(.37f); if (slow) Assert.That(_slow.fillAmount, Is.InRange(.6f, .9f));
            Tick(1); Assert.AreEqual(.6f, _slow.fillAmount); Assert.IsFalse(_bar.IsAnimating);
        }

        [TestCase(Slider.Direction.LeftToRight)] [TestCase(Slider.Direction.RightToLeft)]
        [TestCase(Slider.Direction.BottomToTop)] [TestCase(Slider.Direction.TopToBottom)]
        public void SliderActualRangeWithoutNotify_AndFourDirectionWhiteGeometry(Slider.Direction direction)
        {
            var slider = Rect("Slider", _track).gameObject.AddComponent<Slider>();
            slider.fillRect = Rect("Fill", slider.transform); slider.minValue = -50; slider.maxValue = 150;
            slider.interactable = false; slider.direction = direction;
            int events = 0; slider.onValueChanged.AddListener(_ => events++);
            _bar.SlowBufferEnabled = false; _bar.ConfigureTargets(slider, null, null, null, _white, _track, null);
            _bar.SetValue(100, 100, true); _bar.SetValue(60, 100);
            Assert.That(slider.value, Is.EqualTo(70).Within(.0001)); Assert.AreEqual(0, events);
            bool vertical = (int)direction > 1, reverse = direction == Slider.Direction.RightToLeft || direction == Slider.Direction.TopToBottom;
            Assert.That(vertical ? _white.rectTransform.anchorMin.y : _white.rectTransform.anchorMin.x, Is.EqualTo(reverse ? 0 : .6f).Within(.0001));
            Assert.That(vertical ? _white.rectTransform.anchorMax.y : _white.rectTransform.anchorMax.x, Is.EqualTo(reverse ? .4f : 1).Within(.0001));
        }

        [TestCase(Image.FillMethod.Horizontal, 0)] [TestCase(Image.FillMethod.Horizontal, 1)]
        [TestCase(Image.FillMethod.Vertical, 0)] [TestCase(Image.FillMethod.Vertical, 1)]
        public void FilledImagesSupportBothOrigins(Image.FillMethod method, int origin)
        {
            _main.fillMethod = _slow.fillMethod = method; _main.fillOrigin = _slow.fillOrigin = origin;
            _bar.SetValue(60, 100); Assert.IsNull(_bar.Diagnostic); Assert.AreEqual(.6f, _main.fillAmount);
            bool v = method == Image.FillMethod.Vertical;
            Assert.That(v ? _white.rectTransform.anchorMin.y : _white.rectTransform.anchorMin.x, Is.EqualTo(origin == 0 ? .6f : 0).Within(.0001));
        }

        [TestCase(Slider.Direction.LeftToRight, Image.Type.Filled)]
        [TestCase(Slider.Direction.RightToLeft, Image.Type.Filled)]
        [TestCase(Slider.Direction.BottomToTop, Image.Type.Filled)]
        [TestCase(Slider.Direction.TopToBottom, Image.Type.Filled)]
        [TestCase(Slider.Direction.LeftToRight, Image.Type.Simple)]
        [TestCase(Slider.Direction.RightToLeft, Image.Type.Simple)]
        [TestCase(Slider.Direction.BottomToTop, Image.Type.Simple)]
        [TestCase(Slider.Direction.TopToBottom, Image.Type.Simple)]
        [TestCase(Slider.Direction.LeftToRight, Image.Type.Sliced)]
        [TestCase(Slider.Direction.RightToLeft, Image.Type.Sliced)]
        [TestCase(Slider.Direction.BottomToTop, Image.Type.Sliced)]
        [TestCase(Slider.Direction.TopToBottom, Image.Type.Sliced)]
        public void SliderSpriteMeshAndLossMeshAgreeInFourDirections(Slider.Direction direction, Image.Type type)
        {
            var slider = SpriteSlider(direction, type, out var fill);
            _white.sprite = _sprite; _bar.SlowBufferEnabled = false;
            _bar.ConfigureTargets(slider, null, null, null, _white, _track, null);
            int notifications = 0; slider.onValueChanged.AddListener(_ => notifications++);
            _bar.SetValue(100, 100, true); _bar.SetValue(60, 100);
            Assert.IsNull(_bar.Diagnostic); Assert.That(slider.value, Is.EqualTo(70).Within(.0001));
            Assert.AreEqual(0, notifications); Assert.AreEqual(-50, slider.minValue); Assert.AreEqual(150, slider.maxValue);
            var main = MeshBoundsInTrack(fill); var loss = MeshBoundsInTrack(_white); var track = _track.rect;
            bool vertical = (int)direction > 1;
            bool reverse = direction == Slider.Direction.RightToLeft || direction == Slider.Direction.TopToBottom;
            float mainMin = vertical ? (main.yMin - track.yMin) / track.height : (main.xMin - track.xMin) / track.width;
            float mainMax = vertical ? (main.yMax - track.yMin) / track.height : (main.xMax - track.xMin) / track.width;
            float lossMin = vertical ? (loss.yMin - track.yMin) / track.height : (loss.xMin - track.xMin) / track.width;
            float lossMax = vertical ? (loss.yMax - track.yMin) / track.height : (loss.xMax - track.xMin) / track.width;
            Assert.That(mainMin, Is.EqualTo(reverse ? .4f : 0).Within(.0001));
            Assert.That(mainMax, Is.EqualTo(reverse ? 1 : .6f).Within(.0001));
            Assert.That(lossMin, Is.EqualTo(reverse ? 0 : .6f).Within(.0001));
            Assert.That(lossMax, Is.EqualTo(reverse ? .4f : 1).Within(.0001));
            Assert.That(vertical ? main.width : main.height, Is.EqualTo(vertical ? track.width : track.height).Within(.001));
            Assert.That(vertical ? loss.width : loss.height, Is.EqualTo(vertical ? track.width : track.height).Within(.001));
        }

        [TestCase(true, Image.FillMethod.Horizontal, 0, false)]
        [TestCase(false, Image.FillMethod.Horizontal, 0, false)]
        [TestCase(true, Image.FillMethod.Vertical, 1, false)]
        [TestCase(false, Image.FillMethod.Vertical, 1, false)]
        [TestCase(true, Image.FillMethod.Radial360, 0, false)]
        [TestCase(false, Image.FillMethod.Radial360, 0, false)]
        [TestCase(true, Image.FillMethod.Horizontal, 1, true)]
        [TestCase(false, Image.FillMethod.Horizontal, 1, true)]
        public void InvalidSliderFilledConfigurationStopsOnlyAffectedWritesAndCanRecover(bool mainInvalid,
            Image.FillMethod method, int origin, bool preserveAspect)
        {
            var slider = SpriteSlider(Slider.Direction.RightToLeft, Image.Type.Filled, out var fill);
            _main.fillOrigin = _slow.fillOrigin = 1;
            var text = Text();
            _bar.ConfigureTargets(mainInvalid ? slider : null, mainInvalid ? null : _main,
                mainInvalid ? null : slider, mainInvalid ? _slow : null, _white, _track, text);
            _bar.SetValue(100, 100, true); _bar.SetValue(60, 100); Tick(.1f);
            Assert.IsTrue(_white.enabled);
            fill.fillMethod = method; fill.fillOrigin = origin; fill.preserveAspect = preserveAspect;
            float before = slider.value;
            _bar.SetValue(30, 100); Tick(.01f);
            StringAssert.Contains("Slider fillRect", _bar.Diagnostic);
            StringAssert.StartsWith(mainInvalid ? "主条：" : "慢尾：", _bar.Diagnostic);
            Assert.That(slider.value, Is.EqualTo(before).Within(.0001), "Invalid Slider must not be written.");
            Assert.AreEqual(30, _bar.CurrentValue); Assert.AreEqual("30%", text.text);
            if (mainInvalid) { Assert.IsFalse(_white.enabled); Assert.AreEqual(_baseColor, _white.color); Assert.IsFalse(_bar.IsAnimating); }
            else { Assert.AreEqual(.3f, _main.fillAmount); Assert.IsTrue(_white.enabled); }
            Assert.AreEqual(method, fill.fillMethod); Assert.AreEqual(origin, fill.fillOrigin); Assert.AreEqual(preserveAspect, fill.preserveAspect);
            fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 1; fill.preserveAspect = false;
            _bar.SyncImmediate();
            Assert.IsNull(_bar.Diagnostic); Assert.That(slider.value, Is.EqualTo(10).Within(.0001));
            Assert.IsFalse(_bar.IsAnimating); Assert.IsFalse(_white.enabled); Assert.AreEqual(_baseColor, _white.color);
            Assert.AreEqual("30%", text.text);
        }

        [Test] public void ContinuousLossRestartsFromCurrentTail_SameValueDoesNotRestart()
        {
            _bar.SlowDelay = _bar.WhiteDelay = 0; _bar.SlowDuration = _bar.WhiteDuration = 1;
            _bar.SetValue(80, 100); Tick(.5f); Assert.That(_slow.fillAmount, Is.EqualTo(.9f).Within(.001));
            _bar.SetValue(50, 100); Assert.That(_slow.fillAmount, Is.EqualTo(.9f).Within(.001));
            Assert.That(_white.rectTransform.anchorMax.x, Is.EqualTo(.9f).Within(.001));
            Tick(.5f); _bar.SetValue(50, 100); Tick(.5f); Assert.IsFalse(_bar.IsAnimating); Assert.AreEqual(.5f, _slow.fillAmount);
        }
        [Test] public void FadeNewHitRetiresAlreadyFadedRegion_AndHoldHitsMerge()
        {
            _bar.WhiteMode = EUIProgressWhiteMode.Fade; _bar.SetValue(80, 100); _bar.SetValue(50, 100);
            Assert.AreEqual(1, _white.rectTransform.anchorMax.x); Tick(.19f); Assert.Less(_white.color.a, _baseColor.a);
            _bar.SetValue(30, 100); Assert.AreEqual(.5f, _white.rectTransform.anchorMax.x);
            Assert.AreEqual(.3f, _white.rectTransform.anchorMin.x); Assert.AreEqual(_baseColor, _white.color);
            Tick(1); _bar.SetValue(20, 100); Assert.AreEqual(.3f, _white.rectTransform.anchorMax.x);
        }
        [Test] public void HealingRangeModeSwitchClearAndInactiveSetCancelAllOldEffects()
        {
            _bar.SetValue(80, 100); _bar.SetValue(50, 100); Assert.AreEqual(1, _slow.fillAmount);
            _bar.SetValue(70, 100); Assert.AreEqual(.7f, _slow.fillAmount); Assert.IsFalse(_white.enabled);
            _bar.SetValue(70, 200); Assert.AreEqual(.35f, _slow.fillAmount); Assert.IsFalse(_bar.IsAnimating);
            _bar.SetNormalized(.2f); Assert.AreEqual(.2f, _slow.fillAmount); Assert.AreEqual(1, _bar.MaxValue);
            _root.SetActive(false); _bar.SetNormalized(.1f); Assert.AreEqual(.1f, _slow.fillAmount);
            _root.SetActive(true); Tick(2); Assert.AreEqual(.1f, _main.fillAmount);
            _bar.Clear(); Assert.AreEqual(0, _main.fillAmount); Assert.AreEqual(1, _bar.MaxValue);
        }
        [Test] public void ConfigurationChangesOnlyResetTheirOwnEffect()
        {
            _bar.SetValue(60, 100); Tick(.19f); float white = _white.rectTransform.anchorMax.x;
            _bar.SlowBufferEnabled = false; Assert.AreEqual(white, _white.rectTransform.anchorMax.x);
            _bar.SlowBufferEnabled = true; _bar.SetValue(30, 100); float slow = _slow.fillAmount;
            _bar.WhiteMode = EUIProgressWhiteMode.Fade; Assert.AreEqual(slow, _slow.fillAmount); Assert.IsFalse(_white.enabled);
            _bar.WhiteDelay = float.NaN; Assert.AreEqual(slow, _slow.fillAmount);
            _bar.UseUnscaledTime = false; Assert.IsFalse(_bar.IsAnimating);
        }
        [Test] public void NumericAndTimingBoundariesRemainFiniteAndRecover()
        {
            foreach (float max in new[] { 0, -1, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            { _bar.SetValue(30, max); Assert.AreEqual(0, _bar.MaxValue); Assert.AreEqual(0, _main.fillAmount); }
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -10 })
            { _bar.SetValue(value, 100); Assert.AreEqual(0, _bar.CurrentValue); }
            _bar.SetValue(float.MaxValue, float.MaxValue); Assert.AreEqual(1, _main.fillAmount);
            _bar.SlowDelay = _bar.WhiteDelay = float.NaN; _bar.SlowDuration = _bar.WhiteDuration = -1;
            _bar.SetValue(float.MaxValue / 2, float.MaxValue); Assert.AreEqual(.5f, _slow.fillAmount); Assert.IsFalse(_bar.IsAnimating);
            _bar.SlowDelay = float.MaxValue; Assert.AreEqual(3600, _bar.SlowDelay);
            _bar.SetNormalized(float.PositiveInfinity); Assert.AreEqual(0, _main.fillAmount);
        }
        [Test] public void ConflictIsRejectedBeforeWritingMainGraphic_AndOptionalMissingLayerIsSafe()
        {
            _bar.ConfigureTargets(null, _main, null, _main, _main, _track, null);
            _bar.SetValue(60, 100); Tick(.1f); Assert.AreEqual(.6f, _main.fillAmount); Assert.IsTrue(_main.enabled);
            StringAssert.Contains("共享", _bar.Diagnostic);
            _bar.SlowBufferEnabled = false; _bar.WhiteMode = EUIProgressWhiteMode.Disabled;
            _bar.ConfigureTargets(null, _main, null, null, null, null, null); Assert.IsNull(_bar.Diagnostic);
            _main.fillMethod = Image.FillMethod.Radial360; float old = _main.fillAmount;
            _bar.SetValue(30, 100); Assert.AreEqual(old, _main.fillAmount); StringAssert.Contains("radial", _bar.Diagnostic);
        }
        [Test] public void TextUsesRealValuesFormatsAndDoesNotStealLocalizationKey()
        {
            var text = Text(); _bar.SetValue(60, 100); Assert.AreEqual("60%", text.text);
            _bar.TextPrecision = 2; Assert.AreEqual("60.00%", text.text);
            _bar.TextMode = EUIProgressTextMode.CurrentOverMax; Assert.AreEqual("60.00 / 100.00", text.text);
            _bar.CustomTextFormat = "{0:0.0} / {1:0.0}"; _bar.TextMode = EUIProgressTextMode.Custom;
            Assert.AreEqual("60.0 / 100.0", text.Source);
            foreach (var bad in new[] { "", " ", "{9}", "{", "{0:Q}" })
            { _bar.CustomTextFormat = bad; Assert.AreEqual("60.00%", text.text); }
            text.Key = "owned"; string previous = text.text; _bar.SetValue(30, 100);
            Assert.AreEqual(previous, text.text); Assert.AreEqual("owned", text.Key); StringAssert.Contains("Key", _bar.Diagnostic);
            text.Key = ""; _bar.RefreshText(); Assert.AreEqual("30.00%", text.Source);
            _bar.Clear(); Assert.AreEqual("0.00%", text.text);
        }
        [Test] public void RebindRestoresBorrowedWhiteAndDoesNotRecaptureFadedAlpha()
        {
            _bar.WhiteMode = EUIProgressWhiteMode.Fade; _bar.SetValue(60, 100); Tick(.19f);
            Assert.Less(_white.color.a, _baseColor.a);
            _bar.ConfigureTargets(null, _main, null, _slow, _white, _track, null);
            Assert.IsFalse(_white.enabled); Assert.AreEqual(_baseColor, _white.color);
            _bar.SetValue(30, 100); Assert.AreEqual(_baseColor, _white.color);
            _bar.ConfigureTargets(null, _main, null, _slow, null, null, null);
            Assert.IsFalse(_white.enabled); Assert.AreEqual(_baseColor, _white.color);
        }
        [Test] public void InvalidWhiteHierarchyAndSharedFillNeverDriveMainTransform()
        {
            _bar.SlowBufferEnabled = false;
            _bar.ConfigureTargets(null, _main, null, null, _white, _white.rectTransform, null);
            var before = _main.rectTransform.anchorMax; _bar.SetValue(60, 100);
            Assert.AreEqual(before, _main.rectTransform.anchorMax); StringAssert.Contains("冲突", _bar.Diagnostic);
            var a = Rect("SliderA", _track).gameObject.AddComponent<Slider>(); var b = Rect("SliderB", _track).gameObject.AddComponent<Slider>();
            a.interactable = b.interactable = false; a.fillRect = b.fillRect = Rect("SharedFill", a.transform);
            _bar.WhiteMode = EUIProgressWhiteMode.Disabled; _bar.SlowBufferEnabled = true;
            _bar.ConfigureTargets(a, null, b, null, null, null, null); _bar.SetNormalized(.4f);
            Assert.That(a.value, Is.EqualTo(.4f).Within(.001)); StringAssert.Contains("共享", _bar.Diagnostic);
        }
        [Test] public void ExtremeSliderRangeAndIllegalSettingsKeepFiniteGeometry()
        {
            var slider = Rect("ExtremeSlider", _track).gameObject.AddComponent<Slider>();
            slider.interactable = false; slider.fillRect = Rect("Fill", slider.transform);
            slider.minValue = -float.MaxValue; slider.maxValue = float.MaxValue;
            _bar.SlowBufferEnabled = false; _bar.WhiteMode = EUIProgressWhiteMode.Disabled;
            _bar.ConfigureTargets(slider, null, null, null, null, null, null); _bar.SetNormalized(.75f);
            Assert.IsTrue(float.IsFinite(slider.value)); Assert.AreEqual(.75f, slider.fillRect.anchorMax.x);
            slider.wholeNumbers = true; float previous = slider.value; _bar.SetNormalized(.2f);
            Assert.AreEqual(previous, slider.value); StringAssert.Contains("wholeNumbers", _bar.Diagnostic);
            slider.wholeNumbers = false; slider.minValue = slider.maxValue; _bar.SetNormalized(.4f);
            StringAssert.Contains("min", _bar.Diagnostic);
        }
        [Test] public void MissingLayersZeroTrackAndInvalidDeltaAreSafe()
        {
            _track.sizeDelta = _main.rectTransform.sizeDelta = _slow.rectTransform.sizeDelta = Vector2.zero;
            _bar.SetValue(60, 100); Tick(float.NaN); Tick(float.PositiveInfinity); Tick(-10);
            Assert.AreEqual(1, _slow.fillAmount); Assert.IsTrue(float.IsFinite(_white.rectTransform.anchorMax.x));
            Tick(float.MaxValue); Assert.IsFalse(_bar.IsAnimating);
            Object.DestroyImmediate(_slow.gameObject); Object.DestroyImmediate(_white.gameObject);
            _bar.SetValue(30, 100); Assert.AreEqual(.3f, _main.fillAmount); Assert.IsFalse(_bar.IsAnimating);
        }
        [Test] public void MainDestroyedRetiresDedicatedWhiteAndPreservesRealData()
        {
            _bar.SetValue(60, 100); Assert.IsTrue(_white.enabled);
            Object.DestroyImmediate(_main.gameObject); Tick(.02f);
            Assert.IsFalse(_white.enabled); Assert.AreEqual(_baseColor, _white.color);
            Assert.AreEqual(60, _bar.CurrentValue); Assert.IsFalse(_bar.IsAnimating);
        }
        [Test] public void ExplicitExtensionAndBridgePreserveNativeDetectionAndLegacyWrapper()
        {
            Assert.AreEqual(EUIBinding.WidgetTypes.Image, EUIBindingEditorUtility.DetectWidgetType(_main.gameObject).Type);
            var binding = _root.AddComponent<EUIBinding>();
            EUIBindingEditorUtility.SetBindings(binding, new[] { new EUIBindingEntrySnapshot { Name = "Bar", GameObjectPath = "", WidgetType = EUIBinding.WidgetTypes.Extension, ClassName = nameof(EUIBufferedProgressBar) } });
            var map = new Dictionary<string, Component>(); EUIBindingBridge.PopulateControlMap(binding, map);
            Assert.AreSame(_bar, map["Bar"]);
            var legacy = new EUIProgressBar();
            typeof(EUIComponent).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(legacy, new object[] { _main.gameObject });
            legacy.Value = .5f; Assert.AreEqual(.5f, _main.fillAmount);
        }
        [TestCase(typeof(Slider), EUIBinding.WidgetTypes.ProgressBar)]
        [TestCase(typeof(Image), EUIBinding.WidgetTypes.Image)]
        [TestCase(typeof(EUIImageEx), EUIBinding.WidgetTypes.Extension)]
        public void SameNodeCompanionDoesNotStealNativeAutomaticSelection(Type native, EUIBinding.WidgetTypes expected)
        {
            var go = Rect("NativeWithCompanion", _root.transform).gameObject;
            go.AddComponent(native); var before = EUIBindingEditorUtility.DetectWidgetType(go);
            go.AddComponent<EUIBufferedProgressBar>(); var after = EUIBindingEditorUtility.DetectWidgetType(go);
            Assert.AreEqual(expected, after.Type); Assert.AreEqual(before.Type, after.Type); Assert.AreEqual(before.ClassName, after.ClassName);
            if (native == typeof(Slider))
            {
                var slider = go.GetComponent<Slider>(); slider.minValue = -50; slider.maxValue = 150;
                var legacy = new EUIProgressBar();
                typeof(EUIComponent).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(legacy, new object[] { go });
                legacy.Value = .5f; Assert.AreEqual(.5f, slider.value, "Preserve legacy absolute value behavior even for a non-normalized Slider range.");
            }
        }
    }
}
