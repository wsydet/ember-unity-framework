using System;
using System.IO;
using System.Linq;
using Game.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed class NovelUISkinTests
    {
        private NovelUISkin _source, _skin, _copy;
        private string _folder;
        private GameObject _root;

        [SetUp]
        public void Setup()
        {
            string name = "SkinTest_" + Guid.NewGuid().ToString("N");
            _folder = "Assets/" + name; AssetDatabase.CreateFolder("Assets", name);
            var texture = new Texture2D(16, 16);
            try
            {
                var pixels = Enumerable.Repeat(Color.white, 256).ToArray(); texture.SetPixels(pixels); texture.Apply();
                File.WriteAllBytes(_folder + "/source.png", texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(_folder + "/source.png", ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(_folder + "/source.png");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
            _source = ScriptableObject.CreateInstance<NovelUISkin>(); _source.Id = "source";
            _source.Images.Add(new NovelUISkinImage { Page = "Page", Control = "", Node = "", Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(_folder + "/source.png") });
        }
        [TearDown]
        public void Cleanup()
        {
            if (_root) UnityEngine.Object.DestroyImmediate(_root);
            if (_copy) AssetDatabase.DeleteAsset(NovelUISkinEditorService.ROOT + "/" + _copy.Id);
            if (_skin) AssetDatabase.DeleteAsset(NovelUISkinEditorService.ROOT + "/" + _skin.Id);
            if (_source) UnityEngine.Object.DestroyImmediate(_source);
            if (_folder != null) AssetDatabase.DeleteAsset(_folder);
        }
        private NovelUISkin Create(NovelUISkin source) => NovelUISkinEditorService.Create("test_" + Guid.NewGuid().ToString("N"), "测试", source);

        [Test]
        public void DuplicateOwnsImagesAndDoesNotChangeSourceImporter()
        {
            _skin = Create(_source); _copy = Create(_skin);
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(_source.Images[0].Sprite), AssetDatabase.GetAssetPath(_skin.Images[0].Sprite));
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(_skin.Images[0].Sprite), AssetDatabase.GetAssetPath(_copy.Images[0].Sprite));
            NovelUISkinEditorService.SetAppearance(_copy, _copy.Images[0].Key, Color.red, Image.Type.Sliced, false, 2, new Vector4(2, 2, 2, 2));
            Assert.AreEqual(Vector4.zero, _skin.Images[0].Sprite.border);
            Assert.AreEqual(Color.white, _skin.Images[0].Color);
            Assert.IsTrue(_copy.Images[0].Pending);
        }

        [Test]
        public void ReplaceAndUndoPreserveOriginalSpriteAndInvalidBatchWritesNothing()
        {
            _skin = Create(_source); var old = _skin.Images[0].Sprite;
            int files = Directory.GetFiles(NovelUISkinEditorService.ROOT + "/" + _skin.Id + "/Images").Length;
            Assert.Throws<ArgumentException>(() => NovelUISkinEditorService.ReplaceImages(_skin, new[]
            {
                new NovelUISkinEditorService.Replacement { Key = _skin.Images[0].Key, File = _folder + "/source.png" },
                new NovelUISkinEditorService.Replacement { Key = "missing", File = _folder + "/source.png" }
            }));
            Assert.AreEqual(files, Directory.GetFiles(NovelUISkinEditorService.ROOT + "/" + _skin.Id + "/Images").Length);
            NovelUISkinEditorService.ReplaceImages(_skin, new[] { new NovelUISkinEditorService.Replacement { Key = _skin.Images[0].Key, File = _folder + "/source.png" } });
            Assert.AreNotEqual(old, _skin.Images[0].Sprite); Assert.IsFalse(_skin.Images[0].Pending);
            Assert.IsNotNull(old); Assert.IsNotNull(_source.Images[0].Sprite);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.AreEqual(old, _skin.Images[0].Sprite); Assert.IsTrue(_skin.Images[0].Pending);
        }

        [Test]
        public void SwitchingToEmptySkinAndBaseRestoresAllImageProperties()
        {
            _skin = Create(_source); _copy = Create(_source); _copy.Images.Clear();
            _root = new GameObject("Page", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = _root.GetComponent<Image>(); image.sprite = _source.Images[0].Sprite;
            image.color = Color.blue; image.preserveAspect = true; image.type = Image.Type.Simple;
            _skin.Images[0].Color = Color.red; _skin.Images[0].Type = Image.Type.Sliced; _skin.Images[0].PixelsPerUnitMultiplier = 3;
            Assert.AreEqual(1, NovelUISkinRuntime.Apply(_root, "Page", _skin));
            Assert.AreEqual(Color.red, image.color);
            NovelUISkinRuntime.Apply(_root, "Page", _copy);
            Assert.AreEqual(Color.blue, image.color); Assert.AreEqual(Image.Type.Simple, image.type);
            Assert.IsTrue(image.preserveAspect); Assert.AreEqual(1, image.pixelsPerUnitMultiplier);
            NovelUISkinRuntime.Apply(_root, "Page", _skin); NovelUISkinRuntime.Apply(_root, "Page", null);
            Assert.AreEqual(_source.Images[0].Sprite, image.sprite); Assert.AreEqual(Color.blue, image.color);
        }

        [Test]
        public void InvalidIdentifiersAndOversizedBordersAreRejected()
        {
            Assert.Throws<ArgumentException>(() => NovelUISkinEditorService.Create("../outside", "测试", _source));
            _skin = Create(_source);
            Assert.Throws<InvalidOperationException>(() => NovelUISkinEditorService.Create(_skin.Id, "重复", _source));
            Assert.Throws<ArgumentException>(() => NovelUISkinEditorService.SetAppearance(_skin, _skin.Images[0].Key,
                Color.white, Image.Type.Sliced, false, 1, new Vector4(20, 0, 20, 0)));
            Assert.AreEqual(Vector4.zero, _skin.Images[0].Sprite.border);
        }
    }
}
