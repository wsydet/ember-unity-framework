using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// 状态 ↔ 场景映射条目。
    /// </summary>
    [Serializable]
    public class StateSceneEntry
    {
        [HorizontalGroup("Row")]
        [HideLabel, ReadOnly]
        [GUIColor(0.7f, 0.7f, 0.7f)]
        public string stateName;

        [HorizontalGroup("Row")]
        [HideLabel]
        public EmberSceneField sceneField;

        public StateSceneEntry() { }

        public StateSceneEntry(string stateName, EmberSceneField sceneField)
        {
            this.stateName = stateName;
            this.sceneField = sceneField;
        }
    }

    /// <summary>
    /// 状态 ↔ 场景路径映射表。
    /// 自动创建 + 自动匹配同名场景。未来可视化编辑器的场景选择数据源。
    /// </summary>
    public class EmberSceneMapping : EmberBaseSO
    {
        private const string GROUP = "Scene Mapping";

        [FoldoutGroup("$GROUP", Expanded = true)]
        [BoxGroup("$GROUP/框架场景", ShowLabel = false)]
        [Title("框架场景（始终打开，含 MainCamera / UICamera）")]
        [AssetsOnly]
        public SceneAsset frameworkScene;

        [BoxGroup("$GROUP/映射", ShowLabel = false)]
        [Title("状态 → 场景", "主场景互斥选择，叠加场景可多选。InitState 不在此列出。")]
        [ListDrawerSettings(ShowFoldout = false, ShowIndexLabels = false)]
        public List<StateSceneEntry> entries = new();

        [BoxGroup("$GROUP/映射")]
        [Button("重新扫描状态并匹配场景", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void Rescan()
        {
            PopulateFromStates();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        // ============================================================

        /// <summary>扫描所有 EmberGameState 子类，创建条目并自动匹配同名场景。</summary>
        public void PopulateFromStates()
        {
            // 自动查找 FrameworkScene
            if (frameworkScene == null)
                frameworkScene = TryFindSceneAsset("FrameworkScene");

            entries.Clear();

            var stateTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !IsSystemAssembly(a))
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(EmberGameState).IsAssignableFrom(t))
                .OrderBy(t => t.Name);

            foreach (var type in stateTypes)
            {
                if (type.Name == "InitState") continue; // 无用户场景，跳过

                var sceneName = DeriveSceneName(type.Name);
                var sceneField = TryFindScene(sceneName);
                entries.Add(new StateSceneEntry(type.Name, sceneField));
            }
        }

        /// <summary>同步新增的状态（保留已有的手动赋值）。</summary>
        public void SyncNewStates()
        {
            var stateTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !IsSystemAssembly(a))
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(EmberGameState).IsAssignableFrom(t))
                .Where(t => t.Name != "InitState")
                .Select(t => t.Name)
                .ToHashSet();

            entries.RemoveAll(e => !stateTypes.Contains(e.stateName));

            foreach (var name in stateTypes)
            {
                if (!entries.Any(e => e.stateName == name))
                {
                    var sceneName = DeriveSceneName(name);
                    entries.Add(new StateSceneEntry(name, TryFindScene(sceneName)));
                }
            }
        }

        private static string DeriveSceneName(string stateName)
        {
            if (stateName.EndsWith("State"))
                return stateName[..^5] + "Scene";
            return stateName;
        }

        private static SceneAsset TryFindSceneAsset(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            return null;
        }

        private static EmberSceneField TryFindScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return EmberSceneField.None;

            var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                    return EmberSceneField.FromAssetPath(path);
            }
            return EmberSceneField.None;
        }

        private static bool IsSystemAssembly(System.Reflection.Assembly assembly)
        {
            var name = assembly.GetName().Name;
            if (string.IsNullOrEmpty(name)) return true;
            if (name.StartsWith("Ember") || name.StartsWith("Game")) return false;
            return true;
        }
    }
}
