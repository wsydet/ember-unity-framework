using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    [FilePath("ProjectSettings/EmberBuildScenes.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class EmberBuildSceneDeclarations : ScriptableSingleton<EmberBuildSceneDeclarations>
    {
        [Serializable] internal sealed class Entry { public string owner, guid, lastPath; public bool enabled; }
        [SerializeField] internal List<Entry> entries = new();
        internal void Persist() => Save(true);
    }
    /// <summary>显式发行场景登记。草稿只登记在关卡目录，不调用本服务。</summary>
    public static class EmberBuildSceneRegistry
    {
        #region 外部方法
        /// <summary>保留未知条目、顺序及启用状态；仅引导场景强制启用并置首。</summary>
        public static EditorBuildSettingsScene[] Merge(IEnumerable<EditorBuildSettingsScene> current,
            IEnumerable<string> declared, string bootstrap)
        {
            var result = new List<EditorBuildSettingsScene>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(bootstrap))
            { result.Add(new EditorBuildSettingsScene(bootstrap, true)); seen.Add(bootstrap); }
            foreach (var scene in current)
                if (seen.Add(scene.path)) result.Add(new EditorBuildSettingsScene(scene.path, scene.enabled));
            foreach (var path in declared.OrderBy(p => p, StringComparer.Ordinal))
                if (seen.Add(path)) result.Add(new EditorBuildSettingsScene(path, true));
            return result.ToArray();
        }

        /// <summary>明确将场景加入全局发行集合；独立 BuildProfile 不会被此调用改写。</summary>
        public static void Register(string owner, SceneAsset scene, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(owner) || scene == null) throw new ArgumentException("需要登记所有者与有效场景。");
            string path = AssetDatabase.GetAssetPath(scene);
            string guid = AssetDatabase.AssetPathToGUID(path);
            var store = EmberBuildSceneDeclarations.instance;
            if (store.entries.Any(e => e.guid == guid && e.owner != owner && e.enabled != enabled))
                throw new InvalidOperationException("其他所有者已登记不同启用状态；请先协调场景发行资格。");
            var declaration = store.entries.Find(e => e.guid == guid && e.owner == owner);
            if (declaration == null) { declaration = new EmberBuildSceneDeclarations.Entry { owner = owner, guid = guid }; store.entries.Add(declaration); }
            declaration.lastPath = path; declaration.enabled = enabled; store.Persist();
            var entries = EditorBuildSettings.globalScenes.ToList();
            var existing = entries.Find(s => s.path == path);
            if (existing == null) entries.Add(new EditorBuildSettingsScene(path, enabled));
            else existing.enabled = enabled;
            EditorBuildSettings.globalScenes = entries.ToArray();
            FrameworkSceneBootstrapper.Synchronize();
        }

        /// <summary>退出所有权登记但保留用户列表；删除发行条目必须由作者显式编辑构建集合。</summary>
        public static void Unregister(string owner, string sceneGuid)
        { var store = EmberBuildSceneDeclarations.instance; store.entries.RemoveAll(e => e.owner == owner && e.guid == sceneGuid); store.Persist(); }

        internal static EditorBuildSettingsScene[] ResolveDeclarations(EditorBuildSettingsScene[] current)
        {
            var result = current.Select(s => new EditorBuildSettingsScene(s.path, s.enabled)).ToList();
            var store = EmberBuildSceneDeclarations.instance; bool changed = false;
            foreach (var entry in store.entries)
            {
                string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (string.IsNullOrEmpty(path)) continue; // 失效记录留给诊断，不丢失未知条目。
                var existing = result.Find(s => s.path == path || s.path == entry.lastPath);
                if (existing == null) result.Add(new EditorBuildSettingsScene(path, entry.enabled));
                else existing.path = path; // 保留用户在列表中的启用状态。
                if (entry.lastPath != path) { entry.lastPath = path; changed = true; }
            }
            if (changed) store.Persist();
            return result.ToArray();
        }

        /// <summary>当前构建配置的实际场景来源；独立配置优先于全局列表。</summary>
        public static EditorBuildSettingsScene[] GetEffectiveScenes()
        {
            var profile = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
            return profile != null && profile.overrideGlobalScenes ? profile.scenes : EditorBuildSettings.globalScenes;
        }

        public static string[] ValidateEffectiveScenes(string bootstrap)
        {
            var errors = new List<string>();
            var scenes = GetEffectiveScenes();
            var enabled = scenes.Where(s => s.enabled).ToArray();
            if (enabled.Length == 0 || enabled[0].path != bootstrap) errors.Add("有效构建列表的首个启用场景必须是 FrameworkScene；独立 BuildProfile 需显式维护。");
            foreach (var scene in scenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null) errors.Add("场景引用已失效（保留供修复）：" + scene.path);
            foreach (var entry in EmberBuildSceneDeclarations.instance.entries)
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(entry.guid))) errors.Add("登记场景失效：" + entry.owner + " / " + entry.lastPath);
            return errors.ToArray();
        }
        #endregion
    }
}
