using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Guide.Editor.Tests
{
    /// <summary>反射访问默认程序集中的模板模块，测试程序集不反向改变业务程序集结构。</summary>
    public class GuideEditorEditTests
    {
        private ScriptableObject _asset;
        private Type _model;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            var type = FindType("Game.Module.Guide.GuideDefine");
            _model = FindType("Game.Module.Guide.Editor.GuideEditorModel");
            if (type == null || _model == null) Assert.Ignore("当前模板未包含引导模块编辑器。");
            _asset = ScriptableObject.CreateInstance(type);
            _path = "Assets/GuideEditorTest_" + Guid.NewGuid().ToString("N") + ".asset";
            AssetDatabase.CreateAsset(_asset, _path);
        }

        [TearDown]
        public void TearDown()
        {
            if (_asset) Undo.ClearUndo(_asset);
            if (!string.IsNullOrEmpty(_path)) AssetDatabase.DeleteAsset(_path);
        }

        [Test]
        public void DuplicateStep_DeepCopiesNestedManagedReferences()
        {
            Invoke("AddStep", _asset, -1);
            using (var serialized = new SerializedObject(_asset))
            {
                var condition = Invoke("CreateCondition", 6);
                serialized.FindProperty("guideSteps").GetArrayElementAtIndex(0)
                    .FindPropertyRelative("endConditionsToSuccess").managedReferenceValue = condition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Invoke("AddStep", _asset, 0);
            using (var serialized = new SerializedObject(_asset))
            {
                var steps = serialized.FindProperty("guideSteps");
                Assert.AreEqual(2, steps.arraySize);
                steps.GetArrayElementAtIndex(1).FindPropertyRelative("endConditionsToSuccess")
                    .FindPropertyRelative("conditionParams").FindPropertyRelative("value").intValue = 37;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                Assert.AreEqual(0, steps.GetArrayElementAtIndex(0).FindPropertyRelative("endConditionsToSuccess")
                    .FindPropertyRelative("conditionParams").FindPropertyRelative("value").intValue);
            }
        }

        [Test]
        public void NewStep_IsEmptyRatherThanDuplicatingLastStep_AndCanUndo()
        {
            Invoke("AddStep", _asset, -1);
            using (var serialized = new SerializedObject(_asset))
            {
                serialized.FindProperty("guideSteps").GetArrayElementAtIndex(0)
                    .FindPropertyRelative("startConditionsToSuccess").managedReferenceValue = Invoke("CreateCondition", 6);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            Invoke("AddStep", _asset, -1);
            Undo.FlushUndoRecordObjects();
            using (var serialized = new SerializedObject(_asset))
            {
                Assert.IsNull(serialized.FindProperty("guideSteps").GetArrayElementAtIndex(1)
                    .FindPropertyRelative("startConditionsToSuccess").managedReferenceValue);
            }
            Undo.PerformUndo();
            using (var serialized = new SerializedObject(_asset))
                Assert.AreEqual(1, serialized.FindProperty("guideSteps").arraySize);
        }

        [Test]
        public void Validation_ReportsMissingParameterWithoutChangingAsset()
        {
            Invoke("AddStep", _asset, -1);
            var condition = Invoke("CreateCondition", 6);
            condition.GetType().GetField("conditionParams").SetValue(condition, null);
            using (var serialized = new SerializedObject(_asset))
            {
                serialized.FindProperty("guideSteps").GetArrayElementAtIndex(0)
                    .FindPropertyRelative("startConditionsToSuccess").managedReferenceValue = condition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var before = EditorJsonUtility.ToJson(_asset);
            var issues = ((IEnumerable)Invoke("Validate", _asset)).Cast<string>().ToArray();
            Assert.IsTrue(issues.Any(item => item.Contains("缺少参数")));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(_asset));
        }

        private object Invoke(string method, params object[] arguments)
            => _model.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);

        private static Type FindType(string name)
            => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
    }
}
