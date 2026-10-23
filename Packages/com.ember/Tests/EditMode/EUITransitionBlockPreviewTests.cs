using Ember.UIExtension;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ember.UI.Tests
{
    public sealed class EUITransitionBlockPreviewTests
    {
        [Test]
        public void HorizontalBands_RenderFormalPrefabParametersAndSaveEvidence()
        {
            const string source = "Assets/GameResource/Resources/UI/Common/Prefabs/EUILoadingPanel.prefab";
            string output = System.IO.Path.GetFullPath("docs/dev/evidence/loading-curtain-20261023");
            System.IO.Directory.CreateDirectory(output);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject canvasObject = null, cameraObject = null;
            var contents = PrefabUtility.LoadPrefabContents(source);
            var rt = new RenderTexture(960, 540, 24);
            Texture2D pixels = null;
            var previous = RenderTexture.active;
            try
            {
                canvasObject = new GameObject("EvidenceCanvas", typeof(RectTransform), typeof(Canvas));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
                var sourceBlock = contents.GetComponentInChildren<EUITransitionBlock>(true);
                var block = Object.Instantiate(sourceBlock, canvas.transform, false);
                block.gameObject.SetActive(true);
                var rect = (RectTransform)block.transform;
                rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; rect.localPosition = Vector3.zero;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var settings = new SerializedObject(block);
                settings.FindProperty("_autoGridSize").boolValue = false;
                settings.FindProperty("_fixedColumns").intValue = 3;
                settings.FindProperty("_enterPattern").intValue = (int)EUIBlockOrderPattern.Lines;
                settings.FindProperty("_exitPattern").intValue = (int)EUIBlockOrderPattern.Lines;
                settings.FindProperty("_linesHorizontal").boolValue = true;
                settings.FindProperty("_enterDirection").intValue = (int)EUIBlockDirection.TopLeft;
                settings.FindProperty("_transitionDuration").floatValue = 0.75f;
                settings.FindProperty("_exitDuration").floatValue = 0.6666667f;
                settings.FindProperty("_staggerInterval").floatValue = 0.08f;
                settings.FindProperty("_useBlockTexture").boolValue = false;
                settings.FindProperty("_blockColor").colorValue = Color.black;
                var curves = settings.FindProperty("_curveAnimation");
                foreach (string name in new[] { "XScale", "YScale", "Alpha" })
                    curves.FindPropertyRelative(name).animationCurveValue = AnimationCurve.Constant(0, 1, 1);
                foreach (string name in new[] { "YPosition", "Rotation" })
                    curves.FindPropertyRelative(name).animationCurveValue = AnimationCurve.Constant(0, 1, 0);
                curves.FindPropertyRelative("XPosition").animationCurveValue = AnimationCurve.EaseInOut(0, 3, 1, 0);
                settings.ApplyModifiedPropertiesWithoutUndo();
                cameraObject = new GameObject("EvidenceCamera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.cameraType = CameraType.Game;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true; camera.orthographicSize = 540;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.25f, 0.65f, 0.85f);
                camera.targetTexture = rt;
                camera.scene = scene;
                canvas.worldCamera = camera;
                pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
                var report = new System.Text.StringBuilder("phase,time,blackPixels,totalPixels\n");
                foreach (bool enter in new[] { true, false })
                {
                    if (enter) block.EditorPlayEnterPreview(); else block.EditorPlayExitPreview();
                    Assert.That(block.transform.childCount, Is.EqualTo(9));
                    float duration = enter ? 0.75f : 0.6666667f;
                    for (int step = 0; step <= 4; step++)
                    {
                        float time = duration * step / 4f;
                        foreach (Transform child in block.transform)
                        {
                            var tweens = DG.Tweening.DOTween.TweensByTarget(child, false);
                            if (tweens == null) continue;
                            foreach (var tween in tweens)
                                DG.Tweening.TweenExtensions.Goto(tween, Mathf.Max(0, time - DG.Tweening.TweenExtensions.Delay(tween)), false);
                        }
                        Canvas.ForceUpdateCanvases();
                        foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                            graphic.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
                        camera.Render(); RenderTexture.active = rt;
                        pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); pixels.Apply();
                        string phase = enter ? "enter" : "exit";
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, phase + "-" + step + ".png"), pixels.EncodeToPNG());
                        int black = 0;
                        foreach (var color in pixels.GetPixels32()) if (color.r < 8 && color.g < 8 && color.b < 8) black++;
                        report.AppendLine(phase + "," + time.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + black + ",518400");
                        if (enter && step == 4 || !enter && step == 0) Assert.That(black, Is.GreaterThan(517000));
                        if (enter && step == 0 || !enter && step == 4) Assert.That(black, Is.LessThan(1000));
                    }
                    block.EditorPreviewStop();
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(output, "samples.csv"), report.ToString());
            }
            finally
            {
                RenderTexture.active = previous;
                if (cameraObject != null) cameraObject.GetComponent<Camera>().targetTexture = null;
                if (pixels != null) Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(rt);
                PrefabUtility.UnloadPrefabContents(contents);
                if (canvasObject != null) Object.DestroyImmediate(canvasObject);
                if (cameraObject != null) Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void InspectorButtons_RepeatAndRemovePreviewTweens()
        {
            var go = new GameObject("Inspector preview", typeof(RectTransform));
            try
            {
                var block = go.AddComponent<EUITransitionBlock>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                for (int i = 0; i < 3; i++)
                {
                    typeof(EUITransitionBlock).GetMethod("EditorPreviewEnter", flags).Invoke(block, null);
                    var targets = new System.Collections.Generic.List<Transform>();
                    foreach (Transform child in go.transform) targets.Add(child);
                    typeof(EUITransitionBlock).GetMethod("EditorPreviewExit", flags).Invoke(block, null);
                    block.EditorPreviewStop();
                    Assert.That(go.transform.childCount, Is.Zero);
                    foreach (var target in targets) Assert.That(DG.Tweening.DOTween.IsTweening(target), Is.False);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void PublicPreview_WithoutAwake_RepeatsAndCleansPool()
        {
            var go = new GameObject("Curtain preview", typeof(RectTransform));
            try
            {
                var block = go.AddComponent<EUITransitionBlock>();
                for (int i = 0; i < 3; i++)
                {
                    block.EditorPlayEnterPreview();
                    Assert.That(block.HasActiveBlocks, Is.True);
                    block.EditorPlayExitPreview();
                    block.EditorPreviewStop();
                    Assert.That(go.transform.childCount, Is.Zero);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void FormalLoadingPrefabContents_PublicPreviewInitializesAndUnloads()
        {
            const string path = "Assets/GameResource/Resources/UI/Common/Prefabs/EUILoadingPanel.prefab";
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var block = contents.GetComponentInChildren<EUITransitionBlock>(true);
                Assert.That(block, Is.Not.Null);
                var serialized = new SerializedObject(block);
                serialized.FindProperty("_autoGridSize").boolValue = false;
                serialized.FindProperty("_fixedColumns").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                block.EditorPlayEnterPreview();
                block.EditorPlayExitPreview();
                block.EditorPreviewStop();
                Assert.That(block.transform.childCount, Is.Zero);
                block.EditorPlayEnterPreview(); // unload while preview is active
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }
}
