using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed class NovelLayoutAppearanceTests
    {
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string READER = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";

        [TestCase("Advance")]
        [TestCase("ChoiceTemplate")]
        public void AppearanceDraftSurvivesReloadWithoutChangingAssetOrClickArea(string key)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Game.UI.Editor.NovelGameplayLayoutWindow"))
                .First(t => t != null);
            var hash = AssetDatabase.GetAssetDependencyHash(READER);
            var window = (EditorWindow)ScriptableObject.CreateInstance(type);
            object Invoke(string method) => type.GetMethod(method, FLAGS).Invoke(window, null);
            Dictionary<string, RectTransform> Targets() => (Dictionary<string, RectTransform>)type.GetField("_targets", FLAGS).GetValue(window);
            try
            {
                Assert.IsNotEmpty(Targets(), "布局窗口必须在 visual-novel 编辑模式下成功载入。");
                var advance = Targets()["Advance"];
                var min = advance.anchorMin; var max = advance.anchorMax; var size = advance.sizeDelta;
                var image = Targets()[key].GetComponentsInChildren<Image>(true).Last();
                var originalColor = image.color;
                var originalSprite = image.sprite;
                var color = new Color(.2f, .4f, .6f, .73f);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/Backgrounds/rooftop_dusk.png");
                Assert.IsNotNull(sprite);
                Undo.IncrementCurrentGroup();
                Undo.RecordObject(image, "Appearance test");
                image.sprite = sprite; image.color = color; image.type = Image.Type.Sliced;
                if (PrefabUtility.IsPartOfPrefabInstance(image)) PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                Undo.FlushUndoRecordObjects();
                Invoke("Changed");
                Undo.PerformUndo();
                Assert.AreEqual(originalColor, image.color);
                // Undo may restore Unity's null native object using a different managed wrapper.
                Assert.IsTrue(image.sprite == originalSprite, "撤销后应恢复同一 Unity Sprite（包括空引用）。");
                Undo.PerformRedo();
                Assert.AreEqual(color, image.color);
                Assert.IsTrue(image.sprite == sprite, "重做后应恢复替换的 Sprite。");
                Invoke("LoadContents");
                Assert.IsTrue(window.hasUnsavedChanges);
                var restored = Targets()[key].GetComponentsInChildren<Image>(true).Last();
                Assert.IsTrue(restored.sprite == sprite, "重载草稿后应保留同一 Unity Sprite。");
                Assert.AreEqual(color, restored.color);
                Assert.AreEqual(Image.Type.Sliced, restored.type);
                Assert.AreEqual(min, Targets()["Advance"].anchorMin);
                Assert.AreEqual(max, Targets()["Advance"].anchorMax);
                Assert.AreEqual(size, Targets()["Advance"].sizeDelta);
                var preview = (Dictionary<string, RectTransform>)type.GetField("_previewTargets", FLAGS).GetValue(window);
                Assert.AreEqual(color, preview[key].GetComponentsInChildren<Image>(true).Last().color);
                window.DiscardChanges();
                Assert.IsFalse(window.hasUnsavedChanges);
                Assert.AreEqual(hash, AssetDatabase.GetAssetDependencyHash(READER));
            }
            finally
            {
                window.DiscardChanges();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
