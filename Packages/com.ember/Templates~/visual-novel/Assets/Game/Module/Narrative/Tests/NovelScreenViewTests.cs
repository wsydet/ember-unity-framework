using System.IO;
using System.Linq;
using Ember.UI;
using Ember.UIExtension;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        [TestCase(1920, 1080)]
        [TestCase(1440, 1080)]
        public void E2FormalReaderTwoImagesShakeMaskAndCleanup(int width, int height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab");
            string before = EditorJsonUtility.ToJson(prefab);
            var preview = new PreviewRenderUtility();
            var host = new GameObject("E2 isolated render"); preview.AddSingleGO(host);
            var root = Object.Instantiate(prefab, host.transform, false); var page = new EUIPage(root);
            try
            {
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = preview.camera;
                root.GetComponent<CanvasScaler>().enabled = false;
                foreach (var safe in root.GetComponentsInChildren<EUISafeArea>(true))
                { safe.enabled = false; var r = (RectTransform)safe.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
                var rootRect = (RectTransform)root.transform; rootRect.sizeDelta = new Vector2(width, height);
                rootRect.position = Vector3.zero; rootRect.localScale = Vector3.one;
                EUIBindingBridge.Attach(page, root.GetComponent<EUIBinding>()); page.Logic.OnInit(); root.GetComponent<CanvasGroup>().alpha = 1;
                var view = (INovelView)page.Logic; var screen = (INovelScreenView)page.Logic; var actor = (INovelActorView)page.Logic;
                var opacity = (INovelOpacityView)page.Logic;
                var old = Resources.Load<Sprite>("UI/Module/Narrative/Atlas/LastLight/Portraits/lin_evening");
                var next = Resources.Load<Sprite>("UI/Module/Narrative/Atlas/LastLight/Portraits/lin_smile");
                var day = Resources.Load<Sprite>("UI/Module/Narrative/Atlas/LastLight/Backgrounds/rooftop_dusk");
                var night = Resources.Load<Sprite>("UI/Module/Narrative/Atlas/LastLight/Backgrounds/rooftop_night");
                Assert.IsNotNull(old); Assert.IsNotNull(next); Assert.IsNotNull(day); Assert.IsNotNull(night);
                view.Visual(new NovelCommand("bg", NovelCommandKind.Background), day, 1);
                view.Visual(new NovelCommand("actor", NovelCommandKind.Character, slot: NovelPortraitSlot.Center), old, 1);
                var center = (RectTransform)root.transform.Find("Center"); var originalAnchor = center.anchorMin;
                var primary = center.Find("Picture").GetComponent<Image>(); var secondary = center.Find("CrossFade/Picture").GetComponent<Image>();
                var dialogue = root.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.name == "Body").rectTransform;
                var dialoguePosition = dialogue.anchoredPosition;
                void Capture(string name)
                {
                    Canvas.ForceUpdateCanvases(); preview.BeginStaticPreview(new Rect(0, 0, width / 2, height / 2));
                    var camera = preview.camera; camera.cameraType = CameraType.Game; camera.orthographic = true; camera.orthographicSize = height / 2f;
                    camera.transform.position = new Vector3(0, 0, -10); camera.transform.rotation = Quaternion.identity;
                    camera.nearClipPlane = .1f; camera.farClipPlane = 100; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                    Texture2D image;
                    try { preview.Render(true); } finally { image = preview.EndStaticPreview(); }
                    try
                    {
                        string folder = ".utmp/visual-novel-e2/frames/" + width + "x" + height; Directory.CreateDirectory(folder);
                        File.WriteAllBytes(folder + "/" + name + ".png", image.EncodeToPNG());
                        Assert.Greater(image.GetPixels32().Select(c => c.GetHashCode()).Distinct().Count(), 50);
                    }
                    finally { Object.DestroyImmediate(image); }
                }
                Capture("start");
                screen.BeginCrossFade(NovelTargetKind.Character, NovelPortraitSlot.Center, next);
                screen.BeginCrossFade(NovelTargetKind.Background, default, night);
                screen.SetCrossFade(NovelTargetKind.Character, NovelPortraitSlot.Center, .5f);
                screen.SetCrossFade(NovelTargetKind.Background, default, .5f);
                Assert.AreSame(old, primary.sprite); Assert.AreSame(next, secondary.sprite);
                Assert.AreEqual(.5f, primary.color.a); Assert.AreEqual(.5f, secondary.color.a);
                Capture("middle");
                actor.ApplyActor(new NovelVisualState { Kind = NovelCommandKind.Character, Slot = NovelPortraitSlot.Center, InstanceId = "actor",
                    Offset = new Vector2(-.2f, 0), Mirror = true, Brightness = .55f }, Vector2.zero, 0);
                screen.SetShake(NovelTargetKind.Stage, default, new Vector2(.01f, 0));
                screen.SetShake(NovelTargetKind.Character, NovelPortraitSlot.Center, new Vector2(.02f, 0));
                Assert.AreEqual(originalAnchor.x - .17f, center.anchorMin.x, .0001f);
                Assert.AreEqual(dialoguePosition, dialogue.anchoredPosition); Assert.Less(center.localScale.x, 0);
                Assert.AreEqual(.55f, primary.color.r); Assert.AreEqual(.55f, secondary.color.r);
                opacity.SetOpacity(NovelTargetKind.Stage, default, .5f);
                Assert.AreEqual(.25f, primary.color.a); Assert.AreEqual(.25f, secondary.color.a);
                screen.SetShake(NovelTargetKind.Stage, default, Vector2.zero); screen.SetShake(NovelTargetKind.Character, NovelPortraitSlot.Center, Vector2.zero);
                Assert.AreEqual(originalAnchor.x - .2f, center.anchorMin.x, .0001f);
                screen.EndCrossFade(NovelTargetKind.Character, NovelPortraitSlot.Center); screen.EndCrossFade(NovelTargetKind.Background, default);
                Assert.AreSame(next, primary.sprite); Assert.IsFalse(secondary.enabled); Assert.IsNull(secondary.sprite); Capture("end");
                screen.SetCover(Color.black, false);
                var cover = root.transform.Find("Cover"); var coverImage = cover.Find("Picture").GetComponent<Image>();
                Assert.Greater(cover.GetSiblingIndex(), center.GetSiblingIndex()); Assert.Less(cover.GetSiblingIndex(), root.transform.Find("Animator").GetSiblingIndex());
                Assert.IsFalse(coverImage.raycastTarget); Assert.IsTrue(coverImage.enabled); Assert.AreEqual(Color.black, coverImage.color);
                screen.SetCover(Color.white, true); Assert.AreEqual(root.transform.childCount - 1, cover.GetSiblingIndex());
                view.ClearVisuals(); Assert.IsFalse(coverImage.enabled); Assert.IsNull(primary.sprite); Assert.IsFalse(primary.enabled);
                Assert.AreEqual(originalAnchor, center.anchorMin); Assert.AreEqual(before, EditorJsonUtility.ToJson(prefab));
            }
            finally { page.Logic.OnDispose(); preview.Cleanup(); }
        }
    }
}
