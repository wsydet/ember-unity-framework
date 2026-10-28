using System.IO;
using System.Linq;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EmberWritableFontsTests
    {
        private const string ROOT = "Assets/EmberWritableFontsRegression";

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(ROOT), "不覆盖已有测试目录。");
            AssetDatabase.CreateFolder("Assets", "EmberWritableFontsRegression");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(ROOT);

        [Test]
        public void GeneratedFontsOwnAtlasMaterialAndFallbackAndDoNotModifySources()
        {
            var preset = Resources.Load<EmberFontSkinCatalog>(EmberFontSkins.DefaultResourcePath);
            var sources = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Packages/com.ember/SharedAssets/Fonts" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var bytes = sources.Select(File.ReadAllBytes).ToArray();
            var counts = sources.Select(p => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p).characterTable.Count).ToArray();
            var catalog = Object.Instantiate(preset);
            AssetDatabase.CreateAsset(catalog, ROOT + "/Catalog.asset");
            Assert.AreEqual(4, EmberWritableFonts.EnsureProjectFonts(catalog, ROOT + "/Fonts"));
            Assert.AreEqual(0, EmberWritableFonts.EnsureProjectFonts(catalog, ROOT + "/Fonts"));
            foreach (var skin in catalog.Skins)
            {
                var font = skin.Fonts[0].Font;
                var original = preset.FindSkin(skin.Id).Fonts[0].Font;
                Assert.IsTrue(original != font);
                Assert.AreEqual(original.faceInfo, font.faceInfo);
                Assert.AreEqual(original.atlasPopulationMode, font.atlasPopulationMode);
                Assert.AreEqual(original.isMultiAtlasTexturesEnabled, font.isMultiAtlasTexturesEnabled);
                Assert.IsTrue(original.material != font.material);
                Assert.IsTrue(original.atlasTexture != font.atlasTexture);
                Assert.IsTrue(font.atlasTexture == font.material.mainTexture, "比较 Unity 原生纹理身份，避免托管包装对象差异。");
                Assert.IsTrue(font.TryAddCharacters("验收中文", out string missing, false), missing);
                Assert.Greater(font.characterTable.Count, 0);
                foreach (var fallback in font.fallbackFontAssetTable)
                {
                    Assert.IsTrue(AssetDatabase.GetAssetPath(fallback).StartsWith(ROOT));
                    if (!fallback.characterLookupTable.ContainsKey('★'))
                        Assert.IsTrue(fallback.TryAddCharacters("★", out string fallbackMissing, false), fallbackMissing);
                    Assert.IsTrue(fallback.characterLookupTable.ContainsKey('★'));
                }
            }
            for (int i = 0; i < sources.Length; i++)
            {
                CollectionAssert.AreEqual(bytes[i], File.ReadAllBytes(sources[i]), sources[i]);
                var source = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sources[i]);
                Assert.AreEqual(counts[i], source.characterTable.Count);
                Assert.IsFalse(EditorUtility.IsDirty(source));
            }
        }

        [Test]
        public void FixedPrefabMigrationPreservesComponentIdentityAndMaterialAppearance()
        {
            var preset = Resources.Load<EmberFontSkinCatalog>(EmberFontSkins.DefaultResourcePath);
            var source = preset.Skins[0].Fonts[0].Font;
            var root = new GameObject("FontReference", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var text = root.AddComponent<TextMeshProUGUI>();
                text.font = source;
                text.text = "验收中文";
                text.fontSize = 31;
                text.color = Color.cyan;
                var material = new Material(source.material);
                material.SetFloat("_OutlineWidth", .17f);
                AssetDatabase.CreateAsset(material, ROOT + "/Preset.mat");
                text.fontSharedMaterial = material;
                var input = root.AddComponent<TMP_InputField>();
                input.textComponent = text;
                string path = ROOT + "/Text.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var before = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<TMP_Text>();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(before, out string guid, out long localId);
                Assert.AreEqual(1, EmberWritableFonts.MakePrefabsWritable(ROOT, ROOT + "/Fonts"));
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var after = saved.GetComponent<TMP_Text>();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(after, out string afterGuid, out long afterId);
                Assert.AreEqual(guid, afterGuid);
                Assert.AreEqual(localId, afterId);
                Assert.AreSame(after, saved.GetComponent<TMP_InputField>().textComponent);
                Assert.AreEqual("验收中文", after.text);
                Assert.AreEqual(31, after.fontSize);
                Assert.AreEqual(Color.cyan, after.color);
                Assert.AreEqual(.17f, after.fontSharedMaterial.GetFloat("_OutlineWidth"));
                Assert.IsTrue(after.font.atlasTexture == after.fontSharedMaterial.mainTexture);
                Assert.IsTrue(AssetDatabase.GetAssetPath(after.font).StartsWith(ROOT));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
