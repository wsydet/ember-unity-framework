// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Ember.SceneUI.Integration;
using Ember.UIExtension;

using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.LowLevel;

namespace Ember.SceneUI.PlayModeTests
{
    /// <summary>Editor PlayMode regression using transient font/atlas/material objects only.</summary>
    public class SharedFontAtlasPlayModeTests
    {
        #region 内部参数

        private const string FONT_PATH = "Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi SDF.asset";
        private const string FONT_GUID = "a32ba8ab7d4aa814e8b5f0a267b29b42";
        private const string SOURCE_GUID = "3990ff594a20c3e42a78428e8b921683";
        private const string SYMBOL_PATH = "Packages/com.ember/SharedAssets/Fonts/NotoSansSymbols2/NotoSansSymbols2-Regular SDF.asset";
        private static readonly string[] LABELS =
        {
            "主控中心", "无人机 A · 待命", "小麦 · 成熟", "胡萝卜 · 成熟",
            "锁定田 · 15g · M2 解锁", "水井 · M1 不消耗水",
        };

        private readonly List<GameObject> _objects = new List<GameObject>();
        private TMP_FontAsset _font;
        private TMP_FontAsset _source;
        private byte[] _sourceBytes;
        private TMP_FontAsset _symbolFont;
        private byte[] _symbolSourceBytes;
        private EmberSceneUIEngine _engine;
        private PrefabSceneUIViewHost _host;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private TMP_FontAsset LoadSharedFont()
        {
            _sourceBytes = File.ReadAllBytes(FONT_PATH);
            _source = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            Assert.IsNotNull(_source);
            Assert.AreEqual(FONT_GUID, AssetDatabase.AssetPathToGUID(FONT_PATH));
            Assert.AreEqual(SOURCE_GUID, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_source.sourceFontFile)));
            Assert.AreEqual(AtlasPopulationMode.Dynamic, _source.atlasPopulationMode);
            Assert.IsTrue(_source.isMultiAtlasTexturesEnabled, "Shared Chinese dynamic font must allow overflow atlases.");
            Assert.AreEqual(1024, _source.atlasWidth);
            Assert.AreEqual(1024, _source.atlasHeight);
            Assert.AreEqual(90, _source.faceInfo.pointSize);
            Assert.AreEqual(9, _source.atlasPadding);
            Assert.IsNotNull(_source.material);
            Assert.IsNotNull(_source.material.shader);
            Assert.AreSame(_source.atlasTextures[0], _source.material.mainTexture);
            return _source;
        }

        private void CreateTransientFont(TMP_FontAsset source)
        {
            // Never clone the persistent atlas texture array or add characters to the shared asset.
            _font = TMP_FontAsset.CreateFontAsset(source.sourceFontFile, (int)source.faceInfo.pointSize,
                source.atlasPadding, source.atlasRenderMode, source.atlasWidth, source.atlasHeight,
                source.atlasPopulationMode, false);
            Assert.IsNotNull(_font);
            _font.hideFlags = HideFlags.DontSave;
            Object.Destroy(_font.material);
            _font.material = new Material(source.material) { hideFlags = HideFlags.DontSave };
            _font.material.mainTexture = _font.atlasTextures[0];
            Assert.IsFalse(EditorUtility.IsPersistent(_font));
            Assert.IsFalse(EditorUtility.IsPersistent(_font.material));
        }

        private void CloneSymbolFallback(TMP_FontAsset primary)
        {
            _symbolSourceBytes = File.ReadAllBytes(SYMBOL_PATH);
            var symbol = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SYMBOL_PATH);
            Assert.IsNotNull(symbol);
            CollectionAssert.Contains(primary.fallbackFontAssetTable, symbol,
                "The shipped primary asset must reference the bundled symbol fallback.");
            Assert.IsNotNull(symbol.sourceFontFile);
            Assert.AreEqual(AtlasPopulationMode.Dynamic, symbol.atlasPopulationMode);
            Assert.IsTrue(symbol.isMultiAtlasTexturesEnabled);
            Assert.AreSame(symbol.atlasTextures[0], symbol.material.mainTexture);

            // Exercise the shipped SDF metrics/material, but isolate every mutable atlas from the asset.
            _symbolFont = Object.Instantiate(symbol);
            _symbolFont.hideFlags = HideFlags.DontSave;
            var textures = new Texture2D[symbol.atlasTextures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = Object.Instantiate(symbol.atlasTextures[i]);
                textures[i].hideFlags = HideFlags.DontSave;
            }
            _symbolFont.atlasTextures = textures;
            _symbolFont.material = new Material(symbol.material) { hideFlags = HideFlags.DontSave };
            _symbolFont.material.mainTexture = textures[0];
            _symbolFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            _font.fallbackFontAssetTable = new List<TMP_FontAsset> { _symbolFont };
        }

        private char FillSingleAtlas(out char first)
        {
            Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(_font.sourceFontFile, 90));
            first = '\0';
            int attempted = 0;
            string reserved = string.Join("", LABELS);
            // Bound work; use only source-supported CJK and reserve acceptance text for after overflow.
            for (uint unicode = 0x4E00; unicode <= 0x9FFF && attempted < 512; unicode++)
            {
                if (reserved.IndexOf((char)unicode) >= 0
                    || !FontEngine.TryGetGlyphWithUnicodeValue(unicode, GlyphLoadFlags.LOAD_NO_BITMAP, out var sourceGlyph)
                    || sourceGlyph.index == 0)
                    continue;
                attempted++;
                if (!_font.TryAddCharacters(new[] { unicode }, out uint[] missing, false))
                {
                    CollectionAssert.Contains(missing, unicode);
                    Assert.AreEqual(1, _font.atlasTextureCount, "Negative control must remain single-atlas.");
                    Assert.Greater(_font.characterTable.Count, 0);
                    Assert.IsFalse(_font.characterLookupTable.ContainsKey(unicode));
                    TestContext.WriteLine($"Single atlas full: {_font.characterTable.Count} characters; rejected U+{unicode:X4}.");
                    return (char)unicode;
                }
                if (first == '\0') first = (char)unicode;
            }
            Assert.Fail("Did not reproduce single-atlas capacity exhaustion within 512 supported CJK characters.");
            return '\0';
        }

        private GameObject CreateObject(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            _objects.Add(go);
            return go;
        }

        private SceneUIHandle CreateSceneUI(out ManualSceneUICameraUpdateSource updates, out RectTransform root)
        {
            var canvasObject = CreateObject("AtlasTestCanvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root = canvasObject.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(Screen.width, Screen.height);
            var camera = CreateObject("AtlasTestCamera", typeof(Camera)).GetComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var prefab = CreateObject("AtlasTestItem", typeof(RectTransform));
            prefab.SetActive(false);
            prefab.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 100);
            var binding = prefab.AddComponent<EUIBinding>();
            var serialized = new SerializedObject(binding);
            serialized.FindProperty("isPage").boolValue = false;
            serialized.FindProperty("className").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var labelObject = new GameObject("Status", typeof(RectTransform));
            labelObject.transform.SetParent(prefab.transform, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.rectTransform.sizeDelta = new Vector2(800, 100);
            label.font = _font;
            label.fontSharedMaterial = _font.material;
            label.fontSize = 24;
            label.text = string.Empty;
            label.raycastTarget = false;

            _host = new PrefabSceneUIViewHost(root);
            var key = new SceneUIViewKey(901);
            Assert.IsTrue(_host.Register(key, prefab, prewarmCount: 1, maxPooledCount: 1));
            updates = new ManualSceneUICameraUpdateSource();
            _engine = new EmberSceneUIEngine();
            var context = _engine.RegisterContext(new SceneUIContextDescriptor
            {
                SceneCamera = camera, Canvas = canvas, LayerRoot = root, ViewHost = _host,
                CameraUpdateSource = updates, Visible = true,
                VisibleRegionProvider = new FixedSceneUIVisibleRegionProvider(new Rect(0, 0, Screen.width, Screen.height)),
            });
            Assert.IsTrue(context.IsValid);
            return _engine.Register(new SceneUIRequest
            {
                Context = context, ViewKey = key,
                Anchor = new WorldPositionSceneUIAnchor(new Vector3(0, 0, 10)),
                UpdatePolicy = SceneUIUpdatePolicy.Manual,
                VisibilityPolicy = SceneUIVisibilityPolicy.DefaultHide,
                ScalePolicy = SceneUIScalePolicy.Fixed,
                ViewLifetimePolicy = SceneUIViewLifetimePolicy.RecycleWhenInvisible,
                BusinessVisible = true,
            });
        }

        private void AssertRendered(TextMeshProUGUI label, string value)
        {
            label.text = value;
            label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(label.gameObject.activeInHierarchy);
            Assert.AreEqual(value.Length, label.textInfo.characterCount);
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i])) continue;
                TMP_CharacterInfo character = label.textInfo.characterInfo[i];
                Assert.AreEqual(value[i], character.character);
                Assert.IsTrue(character.isVisible, $"Invisible U+{(int)value[i]:X4}");
                Assert.AreSame(_font, character.fontAsset, "No fallback font may hide a missing glyph.");
                var glyph = _font.characterLookupTable[value[i]].glyph;
                Material material = label.textInfo.meshInfo[character.materialReferenceIndex].material;
                Assert.AreSame(_font.atlasTextures[glyph.atlasIndex], material.mainTexture);
                Assert.AreSame(_font.material.shader, material.shader);
                Assert.AreEqual(_font.material.GetColor("_FaceColor"), material.GetColor("_FaceColor"));
                Assert.AreEqual(_font.material.GetColor("_OutlineColor"), material.GetColor("_OutlineColor"));
                foreach (string property in new[] { "_OutlineWidth", "_FaceDilate", "_GradientScale" })
                    Assert.AreEqual(_font.material.GetFloat(property), material.GetFloat(property), property);
            }
        }

        private void AssertOverflowSubmesh(TextMeshProUGUI label)
        {
            Assert.GreaterOrEqual(label.textInfo.materialCount, 2);
            TMP_SubMeshUI[] submeshes = label.GetComponentsInChildren<TMP_SubMeshUI>(true);
            bool foundOverflow = false;
            foreach (TMP_SubMeshUI submesh in submeshes)
            {
                if (!submesh.sharedMaterial || submesh.sharedMaterial.mainTexture != _font.atlasTextures[1]) continue;
                foundOverflow = true;
                Assert.IsTrue(submesh.isActiveAndEnabled);
                Assert.IsTrue(submesh.gameObject.activeInHierarchy);
                Assert.AreSame(_font, submesh.fontAsset);
                Assert.Greater(submesh.mesh.vertexCount, 0);
                Assert.Greater(submesh.canvasRenderer.materialCount, 0);
            }
            Assert.IsTrue(foundOverflow, "Second-atlas glyphs must create a live TMP_SubMeshUI with atlas 1 material.");
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [UnityTest]
        public IEnumerator SymbolFallback_RendersMissingSymbolsAndKeepsChinesePrimary()
        {
            var primary = LoadSharedFont();
            CreateTransientFont(primary);
            Assert.IsFalse(_font.HasCharacter('▶', false, true),
                "Negative control: the primary source must really lack U+25B6.");
            CloneSymbolFallback(primary);
            const string symbols = "▶◀▲▼▷◁△▽✓✔✕✖★☆●○◆◇■□⚠⏸⏹⏵⏴";
            Assert.IsTrue(_symbolFont.TryAddCharacters(symbols, out string missing, false), missing);

            SceneUIHandle handle = CreateSceneUI(out var updates, out var root);
            Assert.IsTrue(handle.IsValid);
            updates.NotifyCameraUpdated();
            yield return null;
            var label = root.GetComponentInChildren<TextMeshProUGUI>();
            Assert.IsNotNull(label);
            const string value = "主界面 ▶1 ✓完成 ✕取消 ⚠";
            label.text = value;
            label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(value.Length, label.textInfo.characterCount);
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i])) continue;
                TMP_CharacterInfo character = label.textInfo.characterInfo[i];
                Assert.AreEqual(value[i], character.character, "Missing symbols must not become replacement squares.");
                Assert.IsTrue(character.isVisible);
                TMP_FontAsset expected = _font.HasCharacter(value[i], false, false) ? _font : _symbolFont;
                Assert.AreSame(expected, character.fontAsset);
                var glyph = expected.characterLookupTable[value[i]].glyph;
                Material material = label.textInfo.meshInfo[character.materialReferenceIndex].material;
                Assert.AreSame(expected.atlasTextures[glyph.atlasIndex], material.mainTexture);
                Assert.AreSame(expected.material.shader, material.shader);
            }
            Assert.AreSame(_font, label.textInfo.characterInfo[0].fontAsset);
            Assert.AreSame(_symbolFont, label.textInfo.characterInfo[4].fontAsset);
            bool foundSymbolSubmesh = false;
            foreach (TMP_SubMeshUI submesh in label.GetComponentsInChildren<TMP_SubMeshUI>(true))
            {
                if (submesh.fontAsset != _symbolFont) continue;
                foundSymbolSubmesh = true;
                Assert.IsTrue(submesh.isActiveAndEnabled);
                Assert.Greater(submesh.mesh.vertexCount, 0);
                Assert.Greater(submesh.canvasRenderer.materialCount, 0);
            }
            Assert.IsTrue(foundSymbolSubmesh, "Fallback glyphs must create a live symbol submesh.");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SharedFont_PreservesSourceStyleAndAllowsMultipleAtlases()
        {
            LoadSharedFont();
        }

        [UnityTest]
        public IEnumerator Overflow_AllocatesAtlasAndPreservesMaterialsAcrossSceneUIPoolReuse()
        {
            CreateTransientFont(LoadSharedFont());
            char rejected = FillSingleAtlas(out char first);
            // Retry the exact failure on the same full font, without clearing or resizing its atlas.
            _font.isMultiAtlasTexturesEnabled = _source.isMultiAtlasTexturesEnabled;
            Assert.IsTrue(_font.TryAddCharacters(rejected.ToString(), out string missing, false), missing);
            Assert.GreaterOrEqual(_font.atlasTextureCount, 2);
            Assert.AreEqual(1, _font.characterLookupTable[rejected].glyph.atlasIndex);
            Assert.IsTrue(_font.TryAddCharacters(string.Join("", LABELS), out missing, false), missing);
            Assert.Greater(_font.characterLookupTable[0x89E3].glyph.atlasIndex, 0);
            int atlasCount = _font.atlasTextureCount;
            TestContext.WriteLine($"Overflow recovered: {_font.characterTable.Count} characters, {atlasCount} atlases.");

            SceneUIHandle handle = CreateSceneUI(out var updates, out var root);
            Assert.IsTrue(handle.IsValid);
            updates.NotifyCameraUpdated();
            yield return null;
            var label = root.GetComponentInChildren<TextMeshProUGUI>();
            Assert.IsNotNull(label);
            string mixedAtlasText = first.ToString() + rejected;
            foreach (string value in LABELS)
            {
                AssertRendered(label, value);
                AssertRendered(label, mixedAtlasText + " " + value);
                AssertOverflowSubmesh(label);
                Assert.IsTrue(_engine.SetBusinessVisible(handle, false));
                yield return null;
                updates.NotifyCameraUpdated();
                Assert.IsFalse(label.gameObject.activeInHierarchy);
                Assert.AreEqual(1, _host.PooledViewCount);
                foreach (TMP_SubMeshUI sub in label.GetComponentsInChildren<TMP_SubMeshUI>(true))
                    Assert.IsFalse(sub.gameObject.activeInHierarchy, "Hidden items must not leave floating submesh text.");
                Assert.IsTrue(_engine.SetBusinessVisible(handle, true));
                yield return null;
                updates.NotifyCameraUpdated();
                Assert.AreSame(label, root.GetComponentInChildren<TextMeshProUGUI>(), "Pool must reuse the same TMP component.");
                AssertRendered(label, mixedAtlasText + " " + value);
                AssertOverflowSubmesh(label);
                Assert.AreEqual(atlasCount, _font.atlasTextureCount, "Pool reuse must not allocate another atlas for the same text.");
            }
            Assert.GreaterOrEqual(_host.PoolHitCount, 7);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            _engine?.Dispose();
            _engine = null;
            _host?.Dispose();
            _host = null;
            foreach (GameObject go in _objects)
                if (go) Object.Destroy(go);
            _objects.Clear();
            yield return null; // Let TMP submeshes release their derived materials first.
            if (_font)
            {
                foreach (Texture2D texture in _font.atlasTextures)
                    if (texture) Object.Destroy(texture);
                Object.Destroy(_font.material);
                Object.Destroy(_font);
                _font = null;
            }
            if (_symbolFont)
            {
                foreach (Texture2D texture in _symbolFont.atlasTextures)
                    if (texture) Object.Destroy(texture);
                Object.Destroy(_symbolFont.material);
                Object.Destroy(_symbolFont);
                _symbolFont = null;
            }
            yield return null;
            if (_sourceBytes != null)
                CollectionAssert.AreEqual(_sourceBytes, File.ReadAllBytes(FONT_PATH), "Test must not persist dynamic cache into the shared asset.");
            _sourceBytes = null;
            _source = null;
            if (_symbolSourceBytes != null)
                CollectionAssert.AreEqual(_symbolSourceBytes, File.ReadAllBytes(SYMBOL_PATH),
                    "Test must not persist dynamic cache into the symbol fallback asset.");
            _symbolSourceBytes = null;
        }

        #endregion
    }
}
#endif
