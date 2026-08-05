// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 碰撞体贴合工具 —— 把选中物体沿指定方向吸附到最近碰撞体表面。
    /// 右键 Hierarchy 中任意 GameObject → Ember → 碰撞体贴合 → 快速贴合。
    /// </summary>
    public class ColliderSnapperEditor : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(ColliderSnapperEditor);

        protected override string MenuPath => "Ember/Tool/碰撞体贴合工具";
        protected override string WindowTitle => "碰撞体贴合";
        protected override Vector2 WindowSize => new(400, 500);

        private enum SnapDirection { Left, Right, Down, Up, Back, Forward }

        private SnapDirection _dir = SnapDirection.Down;
        private float _offset;
        private float _maxDist = 10f;
        private bool _showPreview = true;
        private LayerMask _layers = -1;
        private List<(Transform target, Vector3 newPos, Bounds bounds)> _preview = new();
        private Vector2 _scrollPos;

        // ======== 菜单入口 ========

        [MenuItem("Ember/Tool/碰撞体贴合工具", false, 150)]
        public static void ShowWindow()
        {
            var win = GetWindow<ColliderSnapperEditor>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        // ---- 右键：GameObject/Ember/碰撞体贴合 ----

        [MenuItem("GameObject/Ember/碰撞体贴合/打开面板", false, 1100)]
        public static void ShowFromContext() => ShowWindow();

        [MenuItem("GameObject/Ember/碰撞体贴合/向下贴合 (Snap Down)", false, 1150)]
        public static void SnapDownContext() => QuickSnap(SnapDirection.Down);

        [MenuItem("GameObject/Ember/碰撞体贴合/向上贴合 (Snap Up)", false, 1151)]
        public static void SnapUpContext() => QuickSnap(SnapDirection.Up);

        [MenuItem("GameObject/Ember/碰撞体贴合/向左贴合 (Snap Left)", false, 1152)]
        public static void SnapLeftContext() => QuickSnap(SnapDirection.Left);

        [MenuItem("GameObject/Ember/碰撞体贴合/向右贴合 (Snap Right)", false, 1153)]
        public static void SnapRightContext() => QuickSnap(SnapDirection.Right);

        [MenuItem("GameObject/Ember/碰撞体贴合/向前贴合 (Snap Forward)", false, 1154)]
        public static void SnapForwardContext() => QuickSnap(SnapDirection.Forward);

        [MenuItem("GameObject/Ember/碰撞体贴合/向后贴合 (Snap Back)", false, 1155)]
        public static void SnapBackContext() => QuickSnap(SnapDirection.Back);

        private static void QuickSnap(SnapDirection dir)
        {
            var gos = Selection.gameObjects;
            if (gos.Length == 0) return;
            Vector3 d = DirVector(dir);
            Undo.RecordObjects(Selection.transforms, "Snap Colliders");
            int count = 0;
            foreach (var go in gos)
            {
                if (TrySnap(go, d, 10f, -1, 0f, out Vector3 pos))
                { go.transform.position = pos; count++; }
            }
            EmberDebug.Log(TAG, $"[Ember] Snapped {count}/{gos.Length} objects ({dir}).");
        }

        // ======== Lifecycle ========

        protected override void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; UpdatePreview(); }
        protected override void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

        protected override void DrawContent()
        {
            EditorGUI.BeginChangeCheck();
            _dir = (SnapDirection)EditorGUILayout.EnumPopup(L10n("Direction", "贴合方向"), _dir);
            _layers = LayerMaskField(L10n("Snap Layers", "吸附层级"), _layers);
            _offset = EditorGUILayout.FloatField(L10n("Offset", "贴合间距"), _offset);
            _maxDist = EditorGUILayout.FloatField(L10n("Max Distance", "最大检测距离"), _maxDist);
            _showPreview = EditorGUILayout.Toggle(L10n("Show Preview", "显示预览线框"), _showPreview);
            if (EditorGUI.EndChangeCheck()) { UpdatePreview(); SceneView.RepaintAll(); }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(L10n(
                "Selected objects will snap to the nearest collider surface on the specified layers.",
                "选中物体会贴到指定层级上最近的碰撞体表面。"), MessageType.Info);

            if (GUILayout.Button(L10n("Execute Snap", "执行贴合"), BigButtonStyle))
                PerformSnap();
        }

        // ======== 预览 ========

        private void UpdatePreview()
        {
            _preview.Clear();
            var gos = Selection.gameObjects;
            if (gos.Length == 0) return;
            Vector3 d = DirVector(_dir);
            foreach (var go in gos)
            {
                if (go == null) continue;
                if (TrySnap(go, d, _maxDist, _layers, _offset, out Vector3 pos))
                    _preview.Add((go.transform, pos, go.TryGetComponent<Collider>(out var c3) ? c3.bounds
                        : go.TryGetComponent<Collider2D>(out var c2) ? c2.bounds : new Bounds(pos, Vector3.one)));
            }
        }

        private void OnSceneGUI(SceneView sv)
        {
            if (!_showPreview || _preview.Count == 0) return;
            if (Event.current.type == EventType.Repaint) UpdatePreview();
            foreach (var (t, pos, bounds) in _preview)
            {
                if (t == null) continue;
                Handles.color = Color.yellow;
                Handles.DrawDottedLine(t.position, pos, 5f);
                Handles.color = new Color(0, 1, 0, 0.5f);
                DrawWireCube(pos + (bounds.center - t.position), bounds.size);
            }
        }

        private void PerformSnap()
        {
            UpdatePreview();
            var gos = Selection.gameObjects;
            if (_preview.Count == 0) { EmberDebug.LogWarning(TAG, "No surface found within max distance."); return; }
            Undo.RecordObjects(Selection.transforms, "Snap Colliders");
            foreach (var (t, pos, _) in _preview) { if (t) t.position = pos; }
            if (_preview.Count < gos.Length)
                EmberDebug.LogWarning(TAG, $"[Ember] Snapped {_preview.Count}/{gos.Length} objects. {gos.Length - _preview.Count} skipped (no surface found).");
            UpdatePreview();
        }

        // ======== 核心算法 ========

        private static bool TrySnap(GameObject go, Vector3 dir, float maxDist, LayerMask layers, float offset, out Vector3 pos)
        {
            pos = go.transform.position;
            var c3 = go.GetComponent<Collider>();
            if (c3) return TrySnap3D(go, c3, dir, maxDist, layers, offset, out pos);
            var c2 = go.GetComponent<Collider2D>();
            if (c2 && dir.z == 0) return TrySnap2D(go, c2, dir, maxDist, layers, offset, out pos);
            return false;
        }

        private static bool TrySnap3D(GameObject go, Collider col, Vector3 dir, float maxDist, LayerMask layers, float offset, out Vector3 pos)
        {
            pos = go.transform.position;
            var b = col.bounds;
            var origin = b.center + Vector3.Scale(b.extents, dir);
            var hits = Physics.RaycastAll(origin, dir, maxDist, layers);
            float best = float.MaxValue;
            RaycastHit? bestHit = null;
            foreach (var h in hits)
            {
                if (h.transform == go.transform || h.transform.IsChildOf(go.transform) || h.collider.isTrigger) continue;
                if (h.distance < best) { best = h.distance; bestHit = h; }
            }
            if (bestHit.HasValue) { pos = go.transform.position + dir * (bestHit.Value.distance + offset); return true; }
            return false;
        }

        private static bool TrySnap2D(GameObject go, Collider2D col, Vector3 dir, float maxDist, LayerMask layers, float offset, out Vector3 pos)
        {
            pos = go.transform.position;
            var b = col.bounds;
            var d2 = new Vector2(dir.x, dir.y);
            var origin = new Vector2(b.center.x, b.center.y) + d2 * new Vector2(b.extents.x, b.extents.y);
            var hits = Physics2D.RaycastAll(origin, d2, maxDist, layers);
            float best = float.MaxValue;
            RaycastHit2D? bestHit = null;
            foreach (var h in hits)
            {
                if (h.collider == null || h.transform == go.transform || h.transform.IsChildOf(go.transform) || h.collider.isTrigger) continue;
                if (h.distance < best) { best = h.distance; bestHit = h; }
            }
            if (bestHit.HasValue) { pos = go.transform.position + (Vector3)(d2 * (bestHit.Value.distance + offset)); return true; }
            return false;
        }

        private static Vector3 DirVector(SnapDirection d) => d switch
        {
            SnapDirection.Left => Vector3.left, SnapDirection.Right => Vector3.right,
            SnapDirection.Down => Vector3.down, SnapDirection.Up => Vector3.up,
            SnapDirection.Back => Vector3.back, _ => Vector3.forward,
        };

        // ======== 工具方法 ========

        private static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var names = new List<string>(); var nums = new List<int>();
            for (int i = 0; i < 32; i++) { string n = LayerMask.LayerToName(i); if (n != "") { names.Add(n); nums.Add(i); } }
            int compact = 0;
            for (int i = 0; i < nums.Count; i++) { if ((mask.value & (1 << nums[i])) != 0) compact |= (1 << i); }
            compact = EditorGUILayout.MaskField(label, compact, names.ToArray());
            int result = 0;
            for (int i = 0; i < nums.Count; i++) { if ((compact & (1 << i)) != 0) result |= (1 << nums[i]); }
            return result;
        }

        private static void DrawWireCube(Vector3 c, Vector3 s)
        {
            Vector3 h = s / 2;
            var p = new[] { c + new Vector3(-h.x,-h.y,-h.z), c + new Vector3(h.x,-h.y,-h.z), c + new Vector3(h.x,h.y,-h.z), c + new Vector3(-h.x,h.y,-h.z),
                            c + new Vector3(-h.x,-h.y,h.z), c + new Vector3(h.x,-h.y,h.z), c + new Vector3(h.x,h.y,h.z), c + new Vector3(-h.x,h.y,h.z) };
            Handles.DrawLine(p[0],p[1]); Handles.DrawLine(p[1],p[2]); Handles.DrawLine(p[2],p[3]); Handles.DrawLine(p[3],p[0]);
            Handles.DrawLine(p[4],p[5]); Handles.DrawLine(p[5],p[6]); Handles.DrawLine(p[6],p[7]); Handles.DrawLine(p[7],p[4]);
            Handles.DrawLine(p[0],p[4]); Handles.DrawLine(p[1],p[5]); Handles.DrawLine(p[2],p[6]); Handles.DrawLine(p[3],p[7]);
        }
    }
}
#endif
