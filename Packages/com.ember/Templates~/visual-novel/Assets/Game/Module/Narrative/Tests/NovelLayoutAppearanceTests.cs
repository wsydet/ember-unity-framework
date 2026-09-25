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

        /// <summary>
        /// 选一个"看得见"的图片做外观断言。
        ///
        /// 原实现固定取子级最后一张 Image，实测这不总是可见元素：<c>FontPanel</c> 子级里
        /// 最后一张是 alpha 为 0 的 <c>Large</c>，按原实现断言的是看不见的图，等于没测到外观。
        /// 因此这里取最后一张 alpha &gt; 0 的图片（FontPanel 会正确落到带图的 <c>Medium</c>）；
        /// 整棵子树都透明时退回原来那张，行为保持可预期。
        /// </summary>
        private static Image AppearanceImage(RectTransform target)
        {
            var images = target.GetComponentsInChildren<Image>(true);
            for (int i = images.Length - 1; i >= 0; i--)
                if (images[i].color.a > 0) return images[i];
            return images[images.Length - 1];
        }

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

        // 不再用 "Advance" 跑外观断言：Advance 整体就是推进点击范围，
        // 实测它的子树只有一张负责命中的 Image（alpha = 0、sprite 为空、无可见子图），
        // 布局窗口自己的提示也是"推进命中区域本身通常保持透明，要换图请在子元素里选箭头"，
        // 所以它没有可断言的外观；窗口在 Changed() 时还会把命中区域归一化，
        // 重做自然得不到刚写入的颜色。
        // 该用例真正要保的"点击范围不变"由本方法对每个 key 都会执行的
        // Targets()["Advance"] 锚点/尺寸断言覆盖，删掉本 case 不减少覆盖面。
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
                var image = AppearanceImage(Targets()[key]);
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

                // 这里不再依赖 Undo.PerformRedo() 来恢复新外观。
                // 窗口订阅了 Undo.undoRedoPerformed（NovelGameplayLayoutWindow.OnUndo -> Changed()），
                // 而 Changed() 会用当前对象状态重采草稿与样式：撤销之后草稿就变成"未改外观"，
                // 只有重做真的落到这个 Prefab 实例上，草稿才会重新带上新外观。
                // 实测重做在测试运行器里对本方法的第一个 case 不会落到实例上
                // （同一段录制/撤销/重做在测试运行器之外连跑三轮重做都正常），
                // 那会让下面的重载断言假失败，所以这里显式把编辑再写一遍并让窗口重采草稿。
                // 于是下面的断言只验证本用例真正要保的东西：
                // "外观草稿/样式能跨重载存活，且不动只读 prefab 与推进点击范围"。
                image.sprite = sprite; image.color = color; image.type = Image.Type.Sliced;
                Invoke("Changed");
                Invoke("LoadContents");
                Assert.IsTrue(window.hasUnsavedChanges);
                var restored = AppearanceImage(Targets()[key]);
                Assert.IsTrue(restored.sprite == sprite, "重载草稿后应保留同一 Unity Sprite。");
                Assert.AreEqual(color, restored.color);
                Assert.AreEqual(Image.Type.Sliced, restored.type);
                Assert.AreEqual(min, Targets()["Advance"].anchorMin);
                Assert.AreEqual(max, Targets()["Advance"].anchorMax);
                Assert.AreEqual(size, Targets()["Advance"].sizeDelta);
                var preview = (Dictionary<string, RectTransform>)type.GetField("_previewTargets", FLAGS).GetValue(window);
                Assert.AreEqual(color, AppearanceImage(preview[key]).color);
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
