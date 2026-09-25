using System;
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
        private sealed class E0ActualResources : INovelResources
        {
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object =>
                new Lease<T> { IsDone = true, Asset = path.StartsWith("Assets/", StringComparison.Ordinal) ? AssetDatabase.LoadAssetAtPath<T>(path) : Resources.Load<T>(path) };
        }

        [Test]
        public void E0FormalReaderSampleFramesAndRestore()
        {
            const string path = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string before = EditorJsonUtility.ToJson(prefab);
            var preview = new PreviewRenderUtility();
            var host = new GameObject("E0 isolated verification"); preview.AddSingleGO(host);
            var root = UnityEngine.Object.Instantiate(prefab, host.transform, false);
            var page = new EUIPage(root); NovelSession session = null, restored = null;
            try
            {
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = preview.camera;
                root.GetComponent<CanvasScaler>().enabled = false;
                foreach (var safe in root.GetComponentsInChildren<EUISafeArea>(true))
                {
                    safe.enabled = false; var rect = (RectTransform)safe.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                var rootRect = (RectTransform)root.transform; rootRect.sizeDelta = new Vector2(1920, 1080);
                rootRect.position = Vector3.zero; rootRect.localScale = Vector3.one;
                EUIBindingBridge.Attach(page, root.GetComponent<EUIBinding>());
                page.Logic.OnInit(); root.GetComponent<CanvasGroup>().alpha = 1;
                var view = (INovelView)page.Logic;
                session = new NovelSession(new NovelNewGameRequest("Assets/Game/Module/Narrative/Tests/Fixtures/PresentationE0/E0StoryFixture.asset"), () => _tables, new E0ActualResources());
                session.AttachView(view); session.Tick(0, 0);
                int frame = 0; PumpAction(session, "intro", ref frame); session.Advance(++frame); session.Advance(++frame);
                PumpAction(session, "parallel", ref frame);
                var left = root.transform.Find("Left").GetComponentInChildren<Image>(true);
                var right = root.transform.Find("Right").GetComponentInChildren<Image>(true);
                var body = root.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.name == "Body");
                float bodyAlpha = body.color.a;
                void Capture(string name)
                {
                    Canvas.ForceUpdateCanvases(); preview.BeginStaticPreview(new Rect(0, 0, 960, 540));
                    var camera = preview.camera; camera.cameraType = CameraType.Game;
                    camera.orthographic = true; camera.orthographicSize = 540;
                    camera.transform.position = new Vector3(0, 0, -10); camera.transform.rotation = Quaternion.identity;
                    camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.06f, .08f, .11f);
                    Texture2D image = null;
                    try { preview.Render(true); }
                    finally { image = preview.EndStaticPreview(); }
                    try
                    {
                        Directory.CreateDirectory(".utmp/visual-novel-e0/frames");
                        File.WriteAllBytes(".utmp/visual-novel-e0/frames/" + name + ".png", image.EncodeToPNG());
                        Assert.Greater(image.GetPixels32().Select(c => c.GetHashCode()).Distinct().Count(), 50, "画面不能是单色空渲染");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(image); }
                }
                Assert.AreEqual(1, left.color.a); Assert.AreEqual(1, right.color.a); Capture("start");
                session.Tick(3, ++frame); Assert.AreEqual(.7f, left.color.a, .001); Assert.AreEqual(.8f, right.color.a, .001); Capture("middle");
                session.Pause("menu"); session.Tick(100, ++frame); Assert.AreEqual(.7f, left.color.a, .001); Capture("paused");
                session.Resume("menu"); session.Tick(20, ++frame); Assert.AreEqual(.2f, left.color.a, .001); Assert.AreEqual(.4f, right.color.a, .001); Capture("end");
                Assert.IsTrue(session.TryCapture(out var save, out var error), error);
                restored = new NovelSession(save, () => _tables, new E0ActualResources());
                restored.Tick(0, ++frame); restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
                session.Dispose(); restored.CommitRestore(view); Capture("restored");
                Assert.AreEqual(.2f, left.color.a, .001); Assert.AreEqual(.4f, right.color.a, .001);
                ((INovelOpacityView)view).SetOpacity(NovelTargetKind.Stage, default, .5f);
                Assert.AreEqual(.1f, left.color.a, .001); Assert.AreEqual(.2f, right.color.a, .001);
                Assert.AreEqual(bodyAlpha, body.color.a); Capture("stage-multiply");
                restored.Dispose(); Assert.IsNull(left.sprite); Assert.IsFalse(left.enabled);
                Assert.AreEqual(before, EditorJsonUtility.ToJson(prefab));
            }
            finally
            {
                session?.Dispose(); restored?.Dispose(); page.Logic.OnDispose(); preview.Cleanup();
            }
        }

        [Test]
        public void E0HideTakesCurrentOpacityAndOccupiedInstanceFails()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Left), Fade("a", "one"),
                new NovelCommand("time", NovelCommandKind.Wait, duration: 1),
                new NovelCommand("hide", NovelCommandKind.Character, duration: 1, instanceId: "one", visualAction: NovelVisualAction.Hide), SayAction("done"));
            int frame = 0; PumpAction(session, "time", ref frame); session.Tick(1, ++frame);
            session.Tick(.25f, ++frame); Assert.AreEqual(.28125f, view.Picture.color.a, .001);
            session.Tick(.75f, ++frame); Assert.IsNull(view.Picture.sprite);
            Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single().Status);
            using var occupied = ActionSession(view, ShowAction("one", NovelPortraitSlot.Left), ShowAction("two", NovelPortraitSlot.Left));
            for (int i = 0; i < 10 && occupied.Snapshot.Error == null; i++) occupied.Tick(0, ++frame);
            StringAssert.Contains("占用", occupied.Snapshot.Error.Message);
        }
    }
}
