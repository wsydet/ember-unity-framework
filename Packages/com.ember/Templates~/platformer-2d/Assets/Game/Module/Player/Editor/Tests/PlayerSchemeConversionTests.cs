using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerSchemeConversionTests
    {
        private string _folder;
        private PlayerConfig _config;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/PlayerSchemeTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
            _config = UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH));
            // 显式构造旧格式夹具，默认项目完成转换后仍覆盖历史适配。
            if (_config.Scheme != null)
            {
                using var source = new SerializedObject(_config.Scheme);
                using var target = new SerializedObject(_config);
                foreach (string field in new[] { "_graph", "_feel", "_powers", "_enableEffects", "_inputChannel", "_moveAction", "_jumpAction", "_dashAction" })
                    target.CopyFromSerializedProperty(source.FindProperty(field));
                target.FindProperty("_scheme").objectReferenceValue = null;
                target.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(_config, _folder + "/Config.asset");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(_folder);

        [Test]
        public void ExplicitConversionPreservesIdentityAndEnvironmentAndClearsLegacySources()
        {
            var original = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            var originalScheme = original.Scheme;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_config));
            var scheme = PlayerSchemeConversion.CreateFromLegacy(_config, _folder + "/Scheme.asset");
            Assert.That(_config.Scheme, Is.Null, "创建方案不隐式设置项目默认");
            PlayerSchemeConversion.BindEquivalent(_config, scheme);
            Assert.That(_config.Graph, Is.SameAs(original.Graph));
            Assert.That(_config.Feel, Is.SameAs(original.Feel));
            Assert.That(_config.Powers, Is.SameAs(original.Powers));
            Assert.That(_config.EnableEffects, Is.EqualTo(original.EnableEffects));
            Assert.That(_config.Scene, Is.EqualTo(original.Scene));
            Assert.That(_config.Spawn, Is.EqualTo(original.Spawn));
            using (var data = new SerializedObject(_config))
            {
                Assert.That(data.FindProperty("_graph").objectReferenceValue, Is.Null);
                Assert.That(data.FindProperty("_feel").objectReferenceValue, Is.Null);
                Assert.That(data.FindProperty("_powers").objectReferenceValue, Is.Null);
            }
            AssetDatabase.SaveAssetIfDirty(_config);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_config), ImportAssetOptions.ForceUpdate);
            Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_config)), Is.EqualTo(guid));
            Assert.That(original.Scheme, Is.SameAs(originalScheme));
            Assert.Throws<InvalidOperationException>(() => _config.SetFeelProfile(original.Feel));
        }

        [Test]
        public void InvalidSchemeDoesNotFallBackToLegacyAndConversionRefusesOverwrite()
        {
            string path = _folder + "/Scheme.asset";
            var scheme = PlayerSchemeConversion.CreateFromLegacy(_config, path);
            Assert.Throws<ArgumentException>(() => PlayerSchemeConversion.CreateFromLegacy(_config, path));
            PlayerSchemeConversion.BindEquivalent(_config, scheme);
            using (var data = new SerializedObject(scheme))
            {
                data.FindProperty("_feel").objectReferenceValue = null;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.That(_config.Feel, Is.Null, "绑定后不能从旧字段填补缺失方案配置");
        }

        [Test]
        public void ConversionRejectsNonEquivalentBindingWithoutChangingTarget()
        {
            var scheme = PlayerSchemeConversion.CreateFromLegacy(_config, _folder + "/Scheme.asset");
            using (var data = new SerializedObject(scheme))
            {
                data.FindProperty("_moveAction").stringValue = "Different";
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            string before = EditorJsonUtility.ToJson(_config);
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeConversion.BindEquivalent(_config, scheme));
            Assert.That(EditorJsonUtility.ToJson(_config), Is.EqualTo(before));
        }
    }
}
