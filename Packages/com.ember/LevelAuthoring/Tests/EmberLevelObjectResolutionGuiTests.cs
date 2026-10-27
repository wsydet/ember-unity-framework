using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ember.LevelAuthoring.Tests
{
    public sealed partial class EmberLevelObjectResolutionTests
    {
        #region 内部参数
        private const BindingFlags INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags STATIC = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type GuiDebugger = typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIViewDebuggerHelper");
        private readonly List<GuiSnapshot> _guiSnapshots = new();
        private Rect _clickedRect;
        private bool _guiShown;

        [Serializable] private sealed class GuiSnapshot
        {
            public string step, sourceId, linkedId, message, objectError, buttonRect;
            public bool messageError, sourceIsPrefab;
            public int children, created, deleted;
            public Vector2 drawnRange;
        }
        [Serializable] private sealed class GuiEvidence { public List<GuiSnapshot> steps; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string EvidencePath(string name)
        {
            string folder = Path.GetFullPath("docs/dev/level-object-resolution-20261027/gui");
            Directory.CreateDirectory(folder); return Path.Combine(folder, name);
        }

        private static object Host(EditorWindow window) => typeof(EditorWindow).GetField("m_Parent", INSTANCE).GetValue(window);
        private static void RepaintNow(EditorWindow window) => typeof(EditorWindow).GetMethod("RepaintImmediately", INSTANCE).Invoke(window, null);
        private static void StopGuiDebugging() => GuiDebugger.GetMethod("StopDebugging", STATIC).Invoke(null, null);

        private void OpenGui()
        {
            _guiSnapshots.Clear();
            // 仅设置临时窗口的已有页签/搜索，保证所选对象按钮在可见区域。
            _window.GetType().GetField("_category", INSTANCE).SetValue(_window, 3);
            _window.GetType().GetField("_search", INSTANCE).SetValue(_window, "__gui-regression-no-materials__");
            _window.position = new Rect(50, 50, 720, 900); _window.ShowUtility(); _guiShown = true; _window.Focus();
            GuiDebugger.GetMethod("DebugWindow", STATIC).Invoke(null, new[] { Host(_window) });
            RepaintNow(_window);
        }

        private static IEnumerable<(string text, Rect rect)> DrawInstructions()
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.IMGUIDrawInstruction");
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
            GuiDebugger.GetMethod("GetDrawInstructions", STATIC).Invoke(null, new object[] { list, false });
            foreach (var item in list)
            {
                var content = (GUIContent)type.GetField("usedGUIContent", INSTANCE).GetValue(item);
                yield return (content?.text ?? "", (Rect)type.GetField("rect", INSTANCE).GetValue(item));
            }
        }

        private void ClickButton(string text)
        {
            _window.Focus(); RepaintNow(_window);
            var matches = DrawInstructions().Where(i => i.text == text).ToArray();
            Assert.AreEqual(1, matches.Length, "必须定位到一个实际已绘制的按钮：" + text);
            _clickedRect = matches[0].rect;
            Assert.That(_clickedRect.center.y, Is.InRange(0, _window.position.height), "按钮必须在窗口可见范围内");
            // 进入真实 OnGUI/GUILayout.Button 分发，不反射调用 DuplicateSelection/DeleteSelection。
            _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = _clickedRect.center });
            _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = _clickedRect.center });
            Undo.FlushUndoRecordObjects(); RepaintNow(_window);
        }

        private void Snapshot(string step)
        {
            _guiSnapshots.Add(new GuiSnapshot
            {
                step = step, sourceId = _source != null ? _source.stableId : null,
                linkedId = _owner.link != null ? _owner.link.stableId : null,
                children = _source != null ? _source.transform.parent.childCount : 0,
                sourceIsPrefab = _source != null && PrefabUtility.IsPartOfAnyPrefab(_source),
                created = _adapter.created, deleted = _adapter.deleted, drawnRange = _adapter.drawnRange,
                message = (string)_window.GetType().GetField("_message", INSTANCE).GetValue(_window),
                messageError = (bool)_window.GetType().GetField("_messageError", INSTANCE).GetValue(_window),
                objectError = (string)_window.GetType().GetField("_objectError", INSTANCE).GetValue(_window),
                buttonRect = _clickedRect.ToString()
            });
        }

        private void SaveEvidence(string name) => File.WriteAllText(EvidencePath(name + ".json"), JsonUtility.ToJson(new GuiEvidence { steps = _guiSnapshots }, true));

        private static Texture2D Capture(EditorWindow window, string name)
        {
            RepaintNow(window);
            int width = Mathf.RoundToInt(window.position.width * EditorGUIUtility.pixelsPerPoint);
            int height = Mathf.RoundToInt(window.position.height * EditorGUIUtility.pixelsPerPoint);
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active;
            try
            {
                // 读取指定 GUIView 的缓冲区，不抓取桌面或使用相机重渲染替代 Handles。
                var host = Host(window);
                host.GetType().GetMethod("GrabPixels", INSTANCE).Invoke(host, new object[] { rt, new Rect(0, 0, width, height) });
                RenderTexture.active = rt;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); var flipped = new Color32[pixels.Length];
                for (int y = 0; y < height; y++) Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
                texture.SetPixels32(flipped); texture.Apply();
                File.WriteAllBytes(EvidencePath(name + ".png"), texture.EncodeToPNG());
                return texture;
            }
            finally { RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); }
        }

        private static void CaptureWindow(EditorWindow window, string name) => UnityEngine.Object.DestroyImmediate(Capture(window, name));

        private static RectInt RangePixels(EditorWindow window, string name)
        {
            var texture = Capture(window, name);
            try
            {
                int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1, count = 0;
                var pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                {
                    var p = pixels[y * texture.width + x];
                    if (p.r < 180 || p.b < 180 || p.g > 80) continue;
                    count++; minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
                Assert.Greater(count, 50, "Scene GUI 缓冲区中应有测试适配器绘制的洋红色范围");
                return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [UnityTest] public IEnumerator GuiButtonsCopyDeleteUndoRedoMaintainIdentityAndReferences()
        {
            OpenGui(); yield return null;
            var parent = _source.transform.parent; var position = _source.transform.localPosition;
            var rotation = _source.transform.localRotation; var scale = _source.transform.localScale;
            try
            {
                Snapshot("before"); CaptureWindow(_window, "buttons-before");
                ClickButton("复制"); yield return null;
                var copy = _owner.link; string id = copy.stableId;
                Assert.AreSame(copy.gameObject, Selection.activeGameObject); Assert.AreNotEqual("A", id);
                Assert.AreEqual(1, _adapter.created); Assert.AreEqual(2, parent.childCount);
                Assert.AreEqual("A", _source.stableId); Assert.AreEqual(position, _source.transform.localPosition);
                Assert.AreEqual(rotation, _source.transform.localRotation); Assert.AreEqual(scale, _source.transform.localScale);
                Assert.AreSame(parent, copy.transform.parent); Assert.IsFalse(PrefabUtility.IsPartOfAnyPrefab(copy));
                Snapshot("copy-click"); CaptureWindow(_window, "buttons-copied");
                Undo.PerformUndo(); yield return null;
                Assert.AreSame(_source, _owner.link); Assert.AreEqual(1, parent.childCount); Snapshot("copy-undo");
                Undo.PerformRedo(); yield return null;
                Assert.AreEqual(id, _owner.link.stableId); Assert.AreEqual(2, parent.childCount); Snapshot("copy-redo");
                Selection.activeGameObject = _owner.link.gameObject; yield return null;
                ClickButton("删除"); yield return null;
                Assert.AreEqual(1, _adapter.deleted); Assert.IsNull(_owner.link); Assert.AreEqual(1, parent.childCount); Snapshot("delete-click");
                Undo.PerformUndo(); yield return null;
                Assert.AreEqual(id, _owner.link.stableId); Assert.AreEqual(2, parent.childCount); Snapshot("delete-undo");
                Undo.PerformRedo(); yield return null;
                Assert.IsNull(_owner.link); Assert.AreEqual(1, parent.childCount); Snapshot("delete-redo");
                SaveEvidence("buttons");
            }
            finally { StopGuiDebugging(); }
        }

        [UnityTest] public IEnumerator GuiButtonCallbackFailuresRollBackAndRenderErrors()
        {
            OpenGui(); yield return null;
            try
            {
                _adapter.failCreate = true; ClickButton("复制"); yield return null;
                Assert.AreSame(_source, _owner.link); Assert.AreEqual(1, _source.transform.parent.childCount);
                Assert.IsTrue((bool)_window.GetType().GetField("_messageError", INSTANCE).GetValue(_window));
                Assert.IsTrue(DrawInstructions().Any(i => i.text.Contains("injected-create")), "创建失败必须绘制错误提示");
                Snapshot("create-failed-rolled-back"); CaptureWindow(_window, "create-failure");
                _adapter.failCreate = false; ClickButton("复制"); yield return null;
                string id = _owner.link.stableId; _adapter.failDelete = true;
                ClickButton("删除"); yield return null;
                Assert.AreEqual(id, _owner.link.stableId); Assert.AreEqual(2, _source.transform.parent.childCount);
                Assert.IsTrue(DrawInstructions().Any(i => i.text.Contains("injected-delete")), "删除失败必须绘制错误提示");
                Snapshot("delete-failed-rolled-back"); CaptureWindow(_window, "delete-failure");
                // 失败事务不能占据一次撤销，也不能破坏之前成功复制的重做。
                Undo.PerformUndo(); yield return null;
                Assert.AreSame(_source, _owner.link); Assert.AreEqual(1, _source.transform.parent.childCount); Snapshot("undo-previous-success");
                Undo.PerformRedo(); yield return null;
                Assert.AreEqual(id, _owner.link.stableId); Assert.AreEqual(2, _source.transform.parent.childCount); Snapshot("redo-previous-success");
                SaveEvidence("failures");
            }
            finally { StopGuiDebugging(); }
        }

        [UnityTest] public IEnumerator GuiSceneRangeRendersComponentUndoRedoAndFailureDiagnostic()
        {
            OpenGui(); yield return null;
            var sceneView = ScriptableObject.CreateInstance<SceneView>();
            try
            {
                _source.transform.localRotation = Quaternion.identity; _source.transform.localScale = Vector3.one;
                _adapter.drawHandles = true;
                sceneView.position = new Rect(50, 50, 720, 650); sceneView.ShowUtility();
                typeof(SceneView).GetProperty("customScene", INSTANCE).SetValue(sceneView, _scene); sceneView.in2DMode = true;
                sceneView.orthographic = true;
                sceneView.LookAtDirect(_source.transform.position, Quaternion.identity, 8);
                sceneView.Repaint(); yield return null; yield return null;
                var first = RangePixels(sceneView, "scene-range-3x7");
                Assert.AreEqual(new Vector2(3, 7), _adapter.drawnRange); Snapshot("range-3x7");
                Undo.IncrementCurrentGroup(); Undo.RecordObject(_source, "更改实际组件范围"); _source.range = new Vector2(8, 2); Undo.FlushUndoRecordObjects();
                sceneView.Repaint(); yield return null; yield return null;
                var second = RangePixels(sceneView, "scene-range-8x2");
                Assert.AreEqual(_source.range, _adapter.drawnRange); Assert.Greater(second.width, first.width * 2); Assert.Less(second.height, first.height / 2); Snapshot("range-8x2");
                Undo.PerformUndo(); sceneView.Repaint(); yield return null; yield return null;
                var undone = RangePixels(sceneView, "scene-range-undo");
                Assert.AreEqual(new Vector2(3, 7), _source.range); Assert.AreEqual(_source.range, _adapter.drawnRange);
                Assert.That(undone.width, Is.EqualTo(first.width).Within(3)); Assert.That(undone.height, Is.EqualTo(first.height).Within(3)); Snapshot("range-undo");
                Undo.PerformRedo(); sceneView.Repaint(); yield return null; yield return null;
                var redone = RangePixels(sceneView, "scene-range-redo");
                Assert.AreEqual(new Vector2(8, 2), _source.range); Assert.That(redone.width, Is.EqualTo(second.width).Within(3)); Snapshot("range-redo");
                _adapter.failDraw = true; sceneView.Repaint(); yield return null; yield return null;
                StringAssert.Contains("injected-draw", (string)_window.GetType().GetField("_objectError", INSTANCE).GetValue(_window));
                CaptureWindow(sceneView, "scene-draw-failure"); RepaintNow(_window);
                Assert.IsTrue(DrawInstructions().Any(i => i.text.Contains("injected-draw"))); CaptureWindow(_window, "window-draw-failure"); Snapshot("draw-failed-diagnostic");
                _adapter.failDraw = false; sceneView.Repaint(); yield return null; yield return null;
                Assert.IsEmpty((string)_window.GetType().GetField("_objectError", INSTANCE).GetValue(_window)); Snapshot("draw-recovered"); SaveEvidence("scene");
                File.WriteAllText(EvidencePath("range-pixels.json"), JsonUtility.ToJson(new RangeEvidence { first = first, second = second, undone = undone, redone = redone }, true));
            }
            finally { _adapter.drawHandles = false; sceneView.Close(); StopGuiDebugging(); }
        }
        [Serializable] private sealed class RangeEvidence { public RectInt first, second, undone, redone; }
        #endregion
    }
}
