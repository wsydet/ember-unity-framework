using System;
using System.IO;
using System.Linq;
using Ember.Table.Editor;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EmberLocalizationSourceEditTests
    {
        private string _root, _id, _path, _outputPath;
        private EmberLocalizationSource _source;

        [SetUp]
        public void SetUp()
        {
            _id = "test_" + Guid.NewGuid().ToString("N");
            _root = "Assets/__" + _id;
            Directory.CreateDirectory(_root);
            _path = _root + "/text.etable.csv";
            _outputPath = EmberLocalizationEditorService.OUTPUT_ROOT + "/" + _id + ".asset";
            File.WriteAllText(_path, "key,zh_Hans,zh_Hant,ja,en\n" + _id + ".hello,你好,,,Hello\n");
            AssetDatabase.Refresh();
            _source = ScriptableObject.CreateInstance<EmberLocalizationSource>();
            _source.TableId = _id; _source.DisplayName = "测试文案";
            _source.Source = AssetDatabase.LoadAssetAtPath<TextAsset>(_path);
            AssetDatabase.CreateAsset(_source, _root + "/Source.asset");
        }
        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_root); AssetDatabase.DeleteAsset(_outputPath);
            EmberLocalization.Reload();
        }
        [Test]
        public void SaveUpdatesRuntimeAndMarksChangedSourceForReview()
        {
            var document = EmberTableSourceDocument.Load(_path);
            EmberLocalizationEditorService.Save(_source, document);
            Assert.IsFalse(document.Dirty);
            Assert.AreEqual("Hello", _source.Output.Entries[0].Values[3]);
            Assert.IsFalse(_source.Reviews[0].Pending);
            document.Rows[0].Values[1] = "你好，世界";
            EmberLocalizationEditorService.Save(_source, document);
            Assert.IsTrue(_source.Reviews[0].Pending);
            Assert.AreEqual("你好，世界", _source.Output.Entries[0].Values[0]);
            Assert.AreEqual("你好，世界", EmberTableSourceDocument.Load(_path).Rows[0].Values[1]);
        }
        [Test]
        public void FailedTableBakeRestoresSourceAndKeepsDraft()
        {
            var definition = ScriptableObject.CreateInstance<EmberTableDefinition>();
            AssetDatabase.CreateAsset(definition, _root + "/InvalidDefinition.asset");
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_source").objectReferenceValue = _source.Source;
            serialized.FindProperty("_tableId").stringValue = _id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _source.Definition = definition;
            string original = File.ReadAllText(_path);
            var document = EmberTableSourceDocument.Load(_path);
            document.Rows[0].Values[1] = "不应写入";
            Assert.Throws<InvalidOperationException>(() => EmberLocalizationEditorService.Save(_source, document));
            Assert.AreEqual(original, File.ReadAllText(_path));
            Assert.IsTrue(document.Dirty);
            Assert.IsFalse(File.Exists(_outputPath));
        }
        [Test]
        public void DuplicateAcrossRegisteredTablesDoesNotWriteSource()
        {
            var other = EmberLocalizationEditorService.Sources().First(s => s != _source);
            var otherDocument = EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(other.Source));
            var document = EmberTableSourceDocument.Load(_path);
            document.Rows[0].Values[0] = otherDocument.Get(otherDocument.Rows[0], other.KeyColumn);
            string original = File.ReadAllText(_path);
            Assert.Throws<InvalidOperationException>(() => EmberLocalizationEditorService.Save(_source, document));
            Assert.AreEqual(original, File.ReadAllText(_path));
        }
        [Test]
        public void WindowStartsCleanAndRetainsDraftThroughSerialization()
        {
            var window = ScriptableObject.CreateInstance<EmberLocalizationWindow>();
            try
            {
                Assert.IsFalse(window.hasUnsavedChanges);
                var field = typeof(EmberSourceEditorWindow).GetField("Document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var document = EmberTableSourceDocument.Load(_path);
                field.SetValue(window, document);
                document.Rows[0].Values[1] = "未保存草稿";
                string serialized = EditorJsonUtility.ToJson(window);
                field.SetValue(window, null);
                EditorJsonUtility.FromJsonOverwrite(serialized, window);
                var restored = (EmberTableSourceDocument)field.GetValue(window);
                Assert.IsTrue(restored.Dirty);
                Assert.AreEqual("未保存草稿", restored.Rows[0].Values[1]);
            }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }
    }
}
