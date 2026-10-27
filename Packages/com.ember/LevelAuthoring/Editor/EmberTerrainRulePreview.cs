using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>只读连接预览板。草稿规则在明确应用前不改变源资产。</summary>
    public sealed class EmberTerrainRulePreview : EditorWindow
    {
        private EmberTerrainTile _terrain;
        private Vector2 _scroll;
        private static readonly string[][] Shapes = {
            new[] { "111111", "111111", "111111" }, new[] { "11000", "11000", "11111" },
            new[] { "11111", "10001", "10001", "11111" }, new[] { "00100" }, new[] { "111111" }, new[] { "111222", "111222" } };
        private static readonly string[] Labels = { "矩形", "L形", "内洞", "孤立格", "单格宽平台", "不同连接组硬边界" };
        private static readonly Vector2Int[] Offsets = { Vector2Int.right, new(0,-1), Vector2Int.left, new(0,1), new(1,-1),new(-1,-1),new(-1,1),new(1,1) };
        private void OnDisable() { if (_terrain != null) DestroyImmediate(_terrain); }
        private void OnGUI()
        {
            if (_terrain == null) return;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var rules = _terrain.Rules;
            for (int s = 0; s < Shapes.Length; s++)
            {
                GUILayout.Label(Labels[s], EditorStyles.boldLabel); var rows = Shapes[s];
                var area = GUILayoutUtility.GetRect(rows[0].Length * 32, rows.Length * 32, GUILayout.ExpandWidth(false));
                for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == '0') continue;
                    int mask = 0;
                    for (int bit = 0; bit < 8; bit++)
                    { var p = new Vector2Int(x,y) + Offsets[bit]; if (p.y >= 0 && p.y < rows.Length && p.x >= 0 && p.x < rows[p.y].Length && rows[p.y][p.x] == rows[y][x]) mask |= 1 << bit; }
                    int index = _terrain.Evaluate(mask, out bool conflict); var sprite = index >= 0 && !conflict && rules[index].sprite != null ? rules[index].sprite : _terrain.Fallback;
                    var rect = new Rect(area.x + x * 32, area.y + y * 32,32,32);
                    if (sprite != null)
                    {
                        var matrix = GUI.matrix;
                        if (index >= 0 && !conflict)
                        {
                            var rule = rules[index]; GUIUtility.RotateAroundPivot(-90 * rule.quarterTurns, rect.center);
                            if (rule.mirrorX) GUIUtility.ScaleAroundPivot(new Vector2(-1, 1), rect.center);
                        }
                        var uv = sprite.rect; uv.x /= sprite.texture.width; uv.width /= sprite.texture.width; uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
                        GUI.DrawTextureWithTexCoords(rect,sprite.texture,uv); GUI.matrix = matrix;
                    }
                    if (index < 0 || conflict) GUI.Label(rect, "!", EditorStyles.boldLabel);
                    if (rows[y][x] == '2') EditorGUI.DrawRect(new Rect(rect.x,rect.y,4,4), Color.magenta);
                }
            }
            foreach (var issue in EmberTerrainEditing.ValidateRules(_terrain).Take(32)) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }
        public static void Show(EmberTerrainTile terrain)
        {
            var window = GetWindow<EmberTerrainRulePreview>("连接规则预览板");
            if (window._terrain != null) DestroyImmediate(window._terrain);
            window._terrain = Instantiate(terrain); window._terrain.hideFlags = HideFlags.HideAndDontSave; window.Show();
        }
    }

    [CustomEditor(typeof(EmberTerrainTile))]
    internal sealed class EmberTerrainTileInspector : UnityEditor.Editor
    {
        private EmberTerrainTile _draft;
        private SerializedObject _serialized;
        private string[] _affected = Array.Empty<string>();
        private void OnEnable() { _draft = Instantiate((EmberTerrainTile)target); _draft.hideFlags = HideFlags.HideAndDontSave; _serialized = new SerializedObject(_draft); }
        private void OnDisable() { _serialized?.Dispose(); if (_draft != null) DestroyImmediate(_draft); }
        public override void OnInspectorGUI()
        {
            _serialized.Update(); DrawPropertiesExcluding(_serialized, "m_Script"); _serialized.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("当前为规则草稿。先预览，再明确应用；不会打开或保存其他关卡。", MessageType.Info);
            if (GUILayout.Button("预览连接与受影响场景"))
            {
                EmberTerrainRulePreview.Show(_draft);
                string path = AssetDatabase.GetAssetPath(target);
                _affected = AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>AssetDatabase.GetDependencies(p).Contains(path)).ToArray();
            }
            foreach (string path in _affected) EditorGUILayout.LabelField(path);
            if (GUILayout.Button("应用规则草稿到源资产"))
            {
                Undo.RecordObject(target,"更新地形规则"); EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_draft),target);
                target.hideFlags = HideFlags.None; EditorUtility.SetDirty(target); AssetDatabase.SaveAssetIfDirty(target);
                // Tile资源为唯一来源；只显式刷新已打开的格子表现，不保存场景。
                for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                    foreach(var root in UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects())
                        foreach(var map in root.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)) map.RefreshAllTiles();
            }
        }
    }
}
