using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Editor;
using TMPro;

namespace Game.Narrative.Tests
{
    public sealed class NovelLayoutAppearanceTests
    {
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string READER = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";

        [TestCase(NovelTextMode.FullScreen, "FullScreen")]
        [TestCase(NovelTextMode.Title, "Title")]
        public void CustomTextLayoutIsUsedAndOrdinaryDialogueIsRestored(NovelTextMode mode, string prefix)
        {
            var hash = AssetDatabase.GetAssetDependencyHash(READER);
            var preview = new PreviewRenderUtility();
            var host = new GameObject("Custom text layout test"); preview.AddSingleGO(host);
            NovelPlaybackView view = null;
            try
            {
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(READER), host.transform);
                var rects = root.GetComponentsInChildren<RectTransform>(true);
                var frame = rects.Single(t => t.name == prefix + "Layout");
                var style = rects.Single(t => t.name == prefix + "Body");
                frame.anchorMin = new Vector2(.2f, .3f); frame.anchorMax = new Vector2(.8f, .9f);
                frame.anchoredPosition = new Vector2(37, -23); frame.sizeDelta = new Vector2(-90, -60);
                style.offsetMin = new Vector2(25, 45); style.offsetMax = new Vector2(-35, -55);
                style.GetComponent<TMP_Text>().color = Color.cyan;
                style.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.BottomRight;
                frame.GetComponent<Image>().color = new Color(.2f, .3f, .4f, .6f);
                var body = rects.Single(t => t.name == "Body").GetComponent<TMP_Text>();
                var dialogue = (RectTransform)body.transform.parent;
                var oldMin = dialogue.anchorMin; var oldPosition = dialogue.anchoredPosition;
                var oldColor = body.color; var oldAlignment = body.alignment;
                view = new NovelPlaybackView(root, preview.camera, new Vector2(1920, 1080));
                view.PrepareText(new NovelCommand("custom", NovelCommandKind.Say, "自定义布局", "custom", textMode: mode), 0);
                view.ShowText("", int.MaxValue); view.Flush();
                Assert.AreEqual(frame.anchorMin, dialogue.anchorMin);
                Assert.AreEqual(frame.anchorMax, dialogue.anchorMax);
                Assert.AreEqual(frame.anchoredPosition, dialogue.anchoredPosition);
                Assert.AreEqual(frame.sizeDelta, dialogue.sizeDelta);
                Assert.AreEqual(style.offsetMin, body.rectTransform.offsetMin);
                Assert.AreEqual(Color.cyan, body.color);
                Assert.AreEqual(TextAlignmentOptions.BottomRight, body.alignment);
                Assert.AreEqual(frame.GetComponent<Image>().color, dialogue.GetComponent<Image>().color);
                ((INovelTextEffectsView)view).SetTextEffects(.5f, .5f);
                Assert.AreEqual(.25f, body.color.a, .001f);
                Assert.AreEqual(.3f, dialogue.GetComponent<Image>().color.a, .001f);
                var advance = rects.Single(t => t.name == "Advance").GetComponent<Image>();
                Assert.AreEqual(.5f, advance.canvasRenderer.GetAlpha(), .001f);
                view.PrepareText(new NovelCommand("normal", NovelCommandKind.Say, "普通对白", "normal"), 0);
                Assert.AreEqual(oldMin, dialogue.anchorMin); Assert.AreEqual(oldPosition, dialogue.anchoredPosition);
                ((INovelTextEffectsView)view).SetTextEffects(1, 1);
                Assert.AreEqual(1, advance.canvasRenderer.GetAlpha());
                Assert.AreEqual(oldColor, body.color); Assert.AreEqual(oldAlignment, body.alignment);
                Assert.AreEqual(hash, AssetDatabase.GetAssetDependencyHash(READER));
            }
            finally { view?.Dispose(); preview.Cleanup(); }
        }

        [TestCase("Advance")]
        [TestCase("ChoiceTemplate")]
        [TestCase("FullScreenLayout")]
        [TestCase("TitleLayout")]
        [TestCase("FontPanel")]
        [TestCase("HistoryPanel")]
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
                Assert.Greater(preview[key].rect.width, 0, "Editable preview must have visible width.");
                Assert.Greater(preview[key].rect.height, 0, "Editable preview must have visible height.");
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
