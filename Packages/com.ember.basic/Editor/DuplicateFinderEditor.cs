// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 重复物体查找工具 —— 扫描当前场景，找到位置/旋转/缩放/Mesh 完全相同的 3D 物体（Ctrl+D 多按的产物）。
    /// </summary>
    public class DuplicateFinderEditor : EmberEditorWindow
    {
        protected override string MenuPath => "Tools/Ember/重复物体查找";
        protected override string WindowTitle => "重复物体查找";
        protected override Vector2 WindowSize => new(500, 600);

        private List<GameObject> _duplicates = new();
        private Vector2 _scrollPos;

        // ======== 菜单 ========

        [MenuItem("Tools/Ember/重复物体查找")]
        public static void ShowWindow()
        {
            var win = GetWindow<DuplicateFinderEditor>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("GameObject/Ember/查找重复物体", false, 1300)]
        public static void QuickScanFromContext()
        {
            var dups = FindDuplicatesInScene();
            if (dups.Count == 0)
            {
                EditorUtility.DisplayDialog("Ember", "未发现重复的 3D 物体。", "OK");
                return;
            }
            Selection.objects = dups.ToArray();
            Debug.Log($"[Ember] Found {dups.Count} duplicate objects, selected in Hierarchy.");
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox(L10n(
                "Scans the active scene for 3D objects with identical position, rotation, scale and mesh.\nOnly checks objects WITHOUT RectTransform (UI elements excluded).",
                "扫描当前场景，找到位置、旋转、缩放、Mesh 完全相同的重复 3D 物体。\n只检测非 UI 物体（无 RectTransform 的物体）。"), MessageType.Info);

            if (GUILayout.Button(new GUIContent(L10n(" Scan Scene for Duplicates", " 扫描场景中的重复物体"), EditorGUIUtility.IconContent("d_SearchIcon").image), GUILayout.Height(40)))
            {
                _duplicates = FindDuplicatesInScene();
                if (_duplicates.Count > 0) Selection.objects = _duplicates.ToArray();
                EditorUtility.DisplayDialog("Ember",
                    _duplicates.Count > 0
                        ? string.Format(L10n("Found {0} duplicate objects.", "找到 {0} 个重复物体。"), _duplicates.Count)
                        : L10n("No duplicate 3D objects found.", "未发现重复的 3D 物体。"), "OK");
            }

            if (_duplicates.Count > 0)
            {
                EditorGUILayout.Space(5);
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button(string.Format(L10n("Delete All {0} Duplicates", "删除全部 {0} 个重复项"), _duplicates.Count), GUILayout.Height(35)))
                {
                    if (EditorUtility.DisplayDialog(L10n("Confirm", "确认删除"),
                        L10n($"Delete {_duplicates.Count} objects?", $"确定删除 {_duplicates.Count} 个重复物体吗？"),
                        L10n("Delete", "删除"), L10n("Cancel", "取消")))
                        DeleteDuplicates();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();

            if (_duplicates.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(string.Format(L10n("Found {0} duplicates:", "找到 {0} 个重复物体:"), _duplicates.Count), EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box", GUILayout.ExpandHeight(true));
            for (int i = 0; i < _duplicates.Count; i++)
            {
                var go = _duplicates[i];
                if (go == null) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{i + 1}. {go.name}", EditorStyles.miniLabel);
                if (GUILayout.Button(L10n("Locate", "定位"), GUILayout.Width(60)))
                { Selection.activeGameObject = go; EditorGUIUtility.PingObject(go); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ======== 核心逻辑 ========

        private static List<GameObject> FindDuplicatesInScene()
        {
            var result = new List<GameObject>();
            var candidates = new List<GameObject>();

            // 只拿当前场景的物体，不用 FindObjectsOfTypeAll
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                CollectNonUIObjects(root.transform, candidates);

            var processed = new HashSet<int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (processed.Contains(i)) continue;
                for (int j = i + 1; j < candidates.Count; j++)
                {
                    if (processed.Contains(j)) continue;
                    if (IsDuplicate(candidates[i], candidates[j]))
                    {
                        result.Add(candidates[j]);
                        processed.Add(j);
                    }
                }
            }
            return result;
        }

        private static void CollectNonUIObjects(Transform t, List<GameObject> list)
        {
            // 跳过 RectTransform（UI 物体不参与检测）
            if (t is not RectTransform && t.gameObject.activeInHierarchy)
                list.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
                CollectNonUIObjects(t.GetChild(i), list);
        }

        private static bool IsDuplicate(GameObject a, GameObject b)
        {
            if (Vector3.Distance(a.transform.position, b.transform.position) > 0.001f) return false;
            if (Quaternion.Angle(a.transform.rotation, b.transform.rotation) > 0.01f) return false;
            if (Vector3.Distance(a.transform.localScale, b.transform.localScale) > 0.001f) return false;

            var mfA = a.GetComponent<MeshFilter>();
            var mfB = b.GetComponent<MeshFilter>();
            return mfA != null && mfB != null ? mfA.sharedMesh == mfB.sharedMesh : mfA == mfB;
        }

        private void DeleteDuplicates()
        {
            int count = 0;
            foreach (var go in _duplicates) { if (go) { Undo.DestroyObjectImmediate(go); count++; } }
            _duplicates.Clear();
            EditorUtility.DisplayDialog("Ember", string.Format(L10n("Removed {0} objects.", "已移除 {0} 个重复物体。"), count), "OK");
        }
    }
}
#endif
