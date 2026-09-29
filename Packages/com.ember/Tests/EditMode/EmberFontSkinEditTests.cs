using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EmberFontSkinEditTests
    {
        private GameObject _root;
        private int _previous;

        [SetUp]
        public void SetUp()
        {
            _previous = EmberFontSkins.ActiveSkinId;
            _root = new GameObject("FontSkinTest", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            EmberFontSkins.SetSkin(_previous);
        }

        [Test]
        public void EachSkinUsesItsOwnSelectionAndFixedTextDoesNotChange()
        {
            var text = _root.AddComponent<TMPEx>();
            text.text = "原文";
            text.Key = "";
            text.fontSize = 37;
            text.SetFontForSkin(1, 2);
            text.SetFontForSkin(2, 3);
            text.SetFontForSkin(3, 1);
            text.SetFontSkin(0, 1);
            foreach (int id in new[] { 1, 2, 3, 1 })
            {
                Assert.IsTrue(EmberFontSkins.SetSkin(id));
                Assert.IsTrue(EmberFontSkins.Catalog.TryGetFont(id, text.GetFontSlotForSkin(id), out var expected));
                Assert.AreSame(expected, text.font);
                Assert.AreEqual("原文", text.text);
                Assert.AreEqual(37, text.fontSize);
                Assert.AreEqual("", text.Key);
            }
            text.SetFontSkin(-1, 1);
            var fixedFont = text.font;
            EmberFontSkins.SetSkin(2);
            Assert.AreSame(fixedFont, text.font);
            Assert.AreEqual(2, text.GetFontSlotForSkin(1));
            Assert.AreEqual(3, text.GetFontSlotForSkin(2));
        }

        [Test]
        public void DisabledTextCatchesUpAndMissingSelectionKeepsExistingFont()
        {
            var text = _root.AddComponent<TMPEx>();
            text.SetFontSkin(0, 1);
            _root.SetActive(false);
            EmberFontSkins.SetSkin(3);
            _root.SetActive(true);
            EmberFontSkins.Catalog.TryGetFont(3, 1, out var expected);
            Assert.AreSame(expected, text.font);
            text.SetFontForSkin(3, int.MaxValue);
            Assert.IsFalse(text.ApplyFontSkin());
            Assert.AreSame(expected, text.font);
            Assert.IsFalse(EmberFontSkins.SetSkin(int.MaxValue));
        }

        [Test]
        public void ConversionPreservesPrefabInputReferenceAndTypography()
        {
            const string path = "Assets/EmberFontSkinConversionTest.prefab";
            Assert.IsFalse(System.IO.File.Exists(path), "测试路径被占用，不覆盖现有资源。");
            var child = new GameObject("Text", typeof(RectTransform));
            child.transform.SetParent(_root.transform);
            var original = child.AddComponent<TextMeshProUGUI>();
            original.text = "输入文字";
            original.fontSize = 29;
            original.color = Color.cyan;
            var input = _root.AddComponent<TMP_InputField>();
            input.textComponent = original;
            try
            {
                Assert.AreEqual(1, EmberFontSkinMigration.ConvertHierarchy(_root));
                PrefabUtility.SaveAsPrefabAsset(_root, path);
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var reference = saved.GetComponent<TMP_InputField>().textComponent;
                Assert.IsInstanceOf<TMPEx>(reference);
                Assert.AreEqual("输入文字", reference.text);
                Assert.AreEqual(29, reference.fontSize);
                Assert.AreEqual(Color.cyan, reference.color);
                Assert.IsTrue(string.IsNullOrEmpty(((TMPEx)reference).Key));
                Assert.AreEqual(0, ((TMPEx)reference).FontSkinId);
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void LegacyConversionCopiesOnlyDisplayPropertiesAndSavesCleanPrefab()
        {
            const string path = "Assets/EmberFontSkinLegacyTest.prefab";
            Assert.IsFalse(System.IO.File.Exists(path));
            var child = new GameObject("Legacy", typeof(RectTransform));
            child.transform.SetParent(_root.transform);
            var legacy = child.AddComponent<UnityEngine.UI.Text>();
            legacy.text = "旧版文本";
            legacy.fontSize = 19;
            legacy.alignment = TextAnchor.MiddleRight;
            try
            {
                Assert.AreEqual(1, EmberFontSkinMigration.ConvertHierarchy(_root));
                PrefabUtility.SaveAsPrefabAsset(_root, path);
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var text = saved.GetComponentInChildren<TMPEx>();
                Assert.AreEqual("旧版文本", text.text);
                Assert.AreEqual(19, text.fontSize);
                Assert.AreEqual(TextAlignmentOptions.Right, text.alignment);
                Assert.IsNull(saved.GetComponentInChildren<UnityEngine.UI.Text>());
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void GenericComponentReplacementRejectsUnrelatedTextTypesBeforeCloning()
        {
            var old = _root.AddComponent<UnityEngine.UI.Text>();
            Assert.IsFalse(EUIComponentReplaceUtility.CanReplace<UnityEngine.UI.Text, TMPEx>(old));
            Assert.Throws<System.ArgumentException>(() => EUIComponentReplaceUtility.Replace<UnityEngine.UI.Text, TMPEx>(old));
            Assert.IsTrue(old);
        }

        [Test]
        public void CatalogRejectsMissingMappingsAndDoesNotUseListPositionAsIdentity()
        {
            var catalog = Object.Instantiate(EmberFontSkins.Catalog);
            try
            {
                Assert.IsTrue(catalog.TryGetFont(1, 1, out var original));
                catalog.Skins.Reverse();
                catalog.Slots.Reverse();
                Assert.IsTrue(catalog.TryGetFont(1, 1, out var reordered));
                Assert.AreSame(original, reordered);
                catalog.FindSkin(1).Fonts.Clear();
                Assert.IsFalse(catalog.Validate(out _));
            }
            finally { Object.DestroyImmediate(catalog); }
        }
    }
}
