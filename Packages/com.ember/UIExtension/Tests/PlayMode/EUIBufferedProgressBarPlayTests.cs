using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ember.UIExtension.Tests
{
    /// <summary>Short isolated frame/Canvas checks. Uses a center-created page when supplied;
    /// after that temporary page is removed the same six rows can be rebuilt as a test fixture.</summary>
    public sealed class EUIBufferedProgressBarPlayTests
    {
        private const string PAGE = "UI/Module/BufferedProgressValidation/Prefabs/EUIBufferedProgressValidationPage";
        private readonly List<Object> _owned = new List<Object>();
        private GameObject _root;
        private EUIBufferedProgressBar[] _bars;
        private EUIPage _page;
        private MethodInfo _setAll;
        private Camera _camera;
        private RenderTexture _rt;
        private float _timeScale;
        private bool _formal;
        private string Evidence(string file)
        {
            string dir = Path.GetFullPath("Temp/EmberBufferedProgressTests"); Directory.CreateDirectory(dir);
            return Path.Combine(dir, file);
        }
        private RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.SetActive(false); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; return rect;
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            _timeScale = Time.timeScale;
            var prefab = Resources.Load<GameObject>(PAGE);
            _formal = prefab != null;
            if (_formal)
            {
                // The saved page already references only our independently created static font.
                _root = Object.Instantiate(prefab); _root.name = "CenterBufferedProgressPage";
                _page = new EUIPage(_root);
                EUIBindingBridge.Attach(_page, _root.GetComponent<EUIBinding>());
                Assert.AreEqual("Game.UI.EUIBufferedProgressValidationPage", _page.Logic.GetType().FullName);
                _bars = (EUIBufferedProgressBar[])_page.Logic.GetType().GetProperty("Bars").GetValue(_page.Logic);
                _setAll = _page.Logic.GetType().GetMethod("SetAll");
                _page.Init(null); _page.PlayShow();
                for (int i = 0; i < 6; i++) Assert.AreSame(_root.transform.Find("m_Bar" + i).GetComponent<EUIBufferedProgressBar>(), _bars[i]);
            }
            else BuildFixture();
            var scaler = _root.GetComponent<CanvasScaler>(); if (scaler) scaler.enabled = false;
            var canvas = _root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = null;
            var r = (RectTransform)_root.transform; r.sizeDelta = new Vector2(1000, 640); r.position = Vector3.zero; r.localScale = Vector3.one;
            foreach (var t in _root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            _rt = new RenderTexture(1000, 640, 24); _rt.Create(); _owned.Add(_rt);
            var cameraObject = new GameObject("BufferedProgressRenderCamera", typeof(Camera)); _owned.Add(cameraObject);
            _camera = cameraObject.GetComponent<Camera>(); _camera.enabled = false; _camera.orthographic = true;
            _camera.orthographicSize = 320; _camera.aspect = 1000f / 640; _camera.transform.position = new Vector3(0, 0, -100);
            _camera.clearFlags = CameraClearFlags.SolidColor; _camera.backgroundColor = new Color(.025f, .03f, .05f);
            _camera.cullingMask = 1 << 31; _camera.targetTexture = _rt; canvas.worldCamera = _camera;
            SetAll(100, 100, true); yield return null;
        }
        private void BuildFixture()
        {
            _root = new GameObject("BufferedProgressFixture", typeof(RectTransform), typeof(Canvas)); _root.SetActive(false);
#if UNITY_EDITOR
            var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi.ttf"));
            font.fallbackFontAssetTable = new List<TMP_FontAsset>(); Assert.IsTrue(font.TryAddCharacters("0123456789% /."));
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            _owned.AddRange(font.atlasTextures); _owned.Add(font.material); _owned.Add(font);
#else
            TMP_FontAsset font = null;
#endif
            var texture = new Texture2D(4, 4); texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); texture.Apply(); _owned.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * .5f); _owned.Add(sprite);
            _bars = new EUIBufferedProgressBar[6];
            for (int i = 0; i < 6; i++)
            {
                var row = Rect("m_Bar" + i, _root.transform, new Vector2(800, 40)); row.anchoredPosition = new Vector2(0, 230 - i * 85);
                var layers = new Image[4];
                for (int j = 0; j < 4; j++)
                {
                    var rect = Rect(new[] { "Background", "Slow", "White", "Main" }[j], row, Vector2.zero);
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
                    image.color = new[] { new Color(.08f, .1f, .14f), new Color(1, .6f, .08f), Color.white, new Color(.1f, .72f, .5f) }[j];
                    if (j == 1 || j == 3) { image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; }
                    rect.gameObject.SetActive(true); layers[j] = image;
                }
                var tr = Rect("Text", row, Vector2.zero); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
                var text = tr.gameObject.AddComponent<TMPEx>(); text.SetFontSkin(-1, 1); text.font = font; text.fontSharedMaterial = font.material;
                text.fontSize = 26; text.alignment = TextAlignmentOptions.Center; text.color = new Color(.02f, .03f, .04f); text.Key = "";
                var bar = row.gameObject.AddComponent<EUIBufferedProgressBar>(); bar.SlowBufferEnabled = i >= 3; bar.WhiteMode = (EUIProgressWhiteMode)(i % 3);
                bar.ConfigureTargets(null, layers[3], null, layers[1], layers[2], row, text); _bars[i] = bar;
                tr.gameObject.SetActive(true); row.gameObject.SetActive(true);
            }
            _root.SetActive(true);
        }
        private void SetAll(float current, float maximum, bool immediate = false)
        {
            if (_setAll != null) _setAll.Invoke(_page.Logic, new object[] { current, maximum, immediate });
            else foreach (var bar in _bars) bar.SetValue(current, maximum, immediate);
        }
        private Image Layer(int i, string name) => _bars[i].transform.Find(name).GetComponent<Image>();
        private Texture2D Render(string file)
        {
            Canvas.ForceUpdateCanvases(); foreach (var bar in _bars) bar.ProgressText.ForceMeshUpdate();
            _camera.Render(); var previous = RenderTexture.active; RenderTexture.active = _rt;
            var texture = new Texture2D(1000, 640, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1000, 640), 0, 0); texture.Apply(); RenderTexture.active = previous;
            File.WriteAllBytes(Evidence(file), texture.EncodeToPNG()); return texture;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale = _timeScale;
            if (_page != null) _page.Logic.OnDispose();
            if (_root) Object.Destroy(_root); yield return null;
            if (_camera) _camera.targetTexture = null;
            if (_rt) _rt.Release();
            for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i]) Object.Destroy(_owned[i]);
            _owned.Clear(); _page = null; _setAll = null; yield return null;
        }

        [UnityTest] public IEnumerator SixCombinations_RealCanvasFramesAndGeneratedPageLifecycle()
        {
            SetAll(60, 100); float start = Time.unscaledTime;
            var frames = new List<Frame>();
            foreach (float target in new[] { 0, .19f, .31f, .68f, 1.08f })
            {
                while (Time.unscaledTime - start < target) yield return null;
                if (target > 0) yield return new WaitForEndOfFrame();
                float elapsed = Time.unscaledTime - start;
                var shot = Render("six-" + target.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                var frame = new Frame { Requested = target, Elapsed = elapsed, FormalPage = _formal, Rows = new Row[6] };
                for (int i = 0; i < 6; i++)
                {
                    var main = Layer(i, "Main"); var slow = Layer(i, "Slow"); var white = Layer(i, "White");
                    Assert.AreEqual(.6f, main.fillAmount); Assert.AreEqual("60%", _bars[i].ProgressText.text);
                    var pixel = shot.GetPixel(260, 534 - i * 85); // Inside real progress, below the text.
                    Assert.Greater(pixel.g, pixel.r + .15f, "Main progress was covered or missing.");
                    if (white.enabled) Assert.That(white.rectTransform.anchorMin.x, Is.EqualTo(.6f).Within(.001));
                    if (target == 0) { Assert.AreEqual(i >= 3 ? 1 : .6f, slow.fillAmount); Assert.AreEqual(i % 3 != 0, white.enabled); }
                    if (target >= .31f) { Assert.IsFalse(white.enabled); Assert.AreEqual(Color.white, white.color); }
                    if (target >= 1.08f) { Assert.AreEqual(.6f, slow.fillAmount); Assert.IsFalse(_bars[i].IsAnimating); }
                    frame.Rows[i] = new Row { Slow = slow.fillAmount, WhiteMin = white.rectTransform.anchorMin.x, WhiteMax = white.rectTransform.anchorMax.x, Alpha = white.color.a, Visible = white.enabled, Text = _bars[i].ProgressText.text, MainPixel = pixel, LossPixel = shot.GetPixel(700, 534 - i * 85) };
                }
                frames.Add(frame); Object.Destroy(shot);
            }
            File.WriteAllText(Evidence("six-frames.json"), JsonUtility.ToJson(new Frames { FramesList = frames.ToArray() }, true));
            if (_formal)
            {
                SetAll(30, 100); _page.HideViewOnly(); Assert.IsTrue(_bars.All(b => !b.IsAnimating));
                _page.RestoreViewOnly(); Assert.IsTrue(_bars.All(b => b.NormalizedValue == .3f && !b.IsAnimating));
            }
        }

        [UnityTest] public IEnumerator ScaledPauseUnscaledPlaybackAndRepeatedSameValue()
        {
            var bar = _bars[3]; bar.SlowDelay = .04f; bar.SlowDuration = .08f;
            Time.timeScale = 0; bar.SetValue(60, 100); yield return new WaitForSecondsRealtime(.18f);
            Assert.AreEqual(.6f, Layer(3, "Slow").fillAmount);
            bar.UseUnscaledTime = false; bar.SetValue(100, 100); bar.SetValue(60, 100);
            yield return new WaitForSecondsRealtime(.18f); Assert.AreEqual(1, Layer(3, "Slow").fillAmount);
            Time.timeScale = 1;
            float deadline = Time.unscaledTime + .25f;
            while (Time.unscaledTime < deadline) { bar.SetValue(60, 100); yield return null; }
            Assert.AreEqual(.6f, Layer(3, "Slow").fillAmount); Assert.IsFalse(bar.IsAnimating);
        }

        [UnityTest] public IEnumerator DisableNeverActiveClearReuseAndTargetDestruction()
        {
            var bar = _bars[5]; bar.SetValue(50, 100); yield return null;
            bar.enabled = false; Assert.AreEqual(.5f, Layer(5, "Slow").fillAmount); Assert.IsFalse(Layer(5, "White").enabled);
            bar.enabled = true; bar.SetValue(30, 100); bar.gameObject.SetActive(false);
            bar.SetValue(80, 100); bar.Clear(); Assert.AreEqual("0%", bar.ProgressText.text);
            bar.SetValue(70, 100); bar.gameObject.SetActive(true); yield return null;
            Assert.AreEqual(.7f, Layer(5, "Slow").fillAmount); Assert.IsFalse(bar.IsAnimating);
            bar.SetValue(30, 100); Object.Destroy(Layer(5, "White").gameObject); yield return null; yield return null;
            Assert.AreEqual(.3f, Layer(5, "Main").fillAmount); Object.Destroy(Layer(5, "Main").gameObject);
            yield return null; yield return null; Assert.IsFalse(bar.IsAnimating);
            var never = new GameObject("NeverActiveBar", typeof(RectTransform)); never.SetActive(false); _owned.Add(never);
            var fresh = never.AddComponent<EUIBufferedProgressBar>(); fresh.SlowBufferEnabled = false; fresh.WhiteMode = EUIProgressWhiteMode.Disabled;
            fresh.SetValue(90, 100); fresh.Clear(); fresh.SetValue(40, 100); never.SetActive(true); yield return null;
            Assert.AreEqual(40, fresh.CurrentValue); Assert.IsFalse(fresh.IsAnimating);
        }
        [UnityTest] public IEnumerator TextSourceSurvivesInstanceLocalizationRefreshWithoutGlobalChanges()
        {
            var bar = _bars[5]; var text = (TMPEx)bar.ProgressText;
            var refresh = typeof(TMPEx).GetMethod("ApplyLocalizedText", BindingFlags.Instance | BindingFlags.NonPublic);
            bar.TextMode = EUIProgressTextMode.CurrentOverMax; bar.SetValue(60, 100);
            Assert.AreEqual("60 / 100", text.Source); refresh.Invoke(text, null);
            yield return null; Assert.AreEqual("60 / 100", text.text); Assert.IsTrue(bar.IsAnimating);
            text.Key = "__buffered_progress_owned_localization_key__";
            string previous = text.text; bar.SetValue(30, 100); Assert.AreEqual(previous, text.text);
            Assert.AreEqual("__buffered_progress_owned_localization_key__", text.Key);
            text.Key = ""; bar.RefreshText(); refresh.Invoke(text, null);
            Assert.AreEqual("30 / 100", text.Source); Assert.AreEqual("30 / 100", text.text);
        }
        [Serializable] private sealed class Frames { public Frame[] FramesList; }
        [Serializable] private sealed class Frame { public float Requested, Elapsed; public bool FormalPage; public Row[] Rows; }
        [Serializable] private sealed class Row { public float Slow, WhiteMin, WhiteMax, Alpha; public bool Visible; public string Text; public Color MainPixel, LossPixel; }
    }
}
