using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.LevelAuthoring.Editor
{
    public enum EmberLevelSeverity { Info, Warning, Error }
    public enum EmberLevelStartMode { Entrance, Position, Checkpoint }

    public sealed class EmberLevelDiagnostic
    {
        public string RuleId { get; }
        public string Message { get; }
        public EmberLevelSeverity Severity { get; }
        public UnityEngine.Object Target { get; }
        public EmberLevelDiagnostic(string rule, string message, EmberLevelSeverity severity, UnityEngine.Object target = null)
        { RuleId = rule; Message = message; Severity = severity; Target = target; }
    }

    [Serializable]
    public sealed class EmberLevelDescriptor
    {
        public string id, displayName, sceneGuid, adapterId;
        public UnityEngine.Object configuration;
        public string ScenePath => UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
    }

    [Serializable]
    public sealed class EmberLevelTrialRequest
    {
        public string requestId, adapterId, levelId, sceneGuid, configurationGuid, checkpointId, configurationHash;
        public EmberLevelStartMode mode;
        public Vector2 position;
    }

    /// <summary>项目提供关卡事实、身份与合法初始化；通用包不解释产品进度。</summary>
    public interface IEmberLevelAdapter
    {
        string Id { get; }
        string UnavailableReason { get; }
        IEnumerable<EmberLevelDescriptor> Discover();
        IEnumerable<EmberLevelDiagnostic> Validate(EmberLevelDescriptor level, EmberLevelTrialRequest trial);
        EmberLevelDescriptor Create(string folder, string name, EmberLevelDescriptor source);
        void BeginTrial(EmberLevelTrialRequest frozen);
        string TrialStatus { get; }
        bool TrialReady { get; }
        bool IsCleaning { get; }
        void StopTrial();
    }

    /// <summary>机关身份与关联编辑必须由项目适配器维护，Undo组由调用者提供。</summary>
    public interface IEmberLevelObjectAdapter
    {
        string Id { get; }
        void ValidatePlacement(EmberLevelMaterial material, Scene scene);
        void OnCreated(GameObject instance, bool duplicate);
        void BeforeDelete(GameObject instance);
        IEnumerable<EmberLevelDiagnostic> Validate(GameObject instance);
        void DrawScene(GameObject instance);
    }

    public static class EmberLevelAdapters
    {
        #region 内部参数
        private static readonly Dictionary<string, IEmberLevelAdapter> Levels = new();
        private static readonly Dictionary<string, IEmberLevelObjectAdapter> Objects = new();
        private static readonly Dictionary<string, Func<GameObject, IEmberLevelObjectAdapter>> ObjectResolvers = new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Register(IEmberLevelAdapter adapter) { Levels[adapter.Id] = adapter; }
        public static void Register(IEmberLevelObjectAdapter adapter) { Objects[adapter.Id] = adapter; }
        public static void Unregister(IEmberLevelAdapter adapter) { if (adapter != null && Get(adapter.Id) == adapter) Levels.Remove(adapter.Id); }
        public static void Unregister(IEmberLevelObjectAdapter adapter) { if (adapter != null && GetObject(adapter.Id) == adapter) Objects.Remove(adapter.Id); }
        public static IEmberLevelAdapter Get(string id) => id != null && Levels.TryGetValue(id, out var value) ? value : null;
        public static IEmberLevelObjectAdapter GetObject(string id) => id != null && Objects.TryGetValue(id, out var value) ? value : null;
        public static IEnumerable<IEmberLevelAdapter> All => Levels.Values;
        public static IEnumerable<IEmberLevelObjectAdapter> ObjectAdapters => Objects.Values;

        /// <summary>登记只读场景对象解析器。返回 null 表示不匹配；匹配时必须返回已登记的适配器实例。</summary>
        public static void RegisterObjectResolver(string id, Func<GameObject, IEmberLevelObjectAdapter> resolver)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("对象解析器需要稳定ID。", nameof(id));
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (ObjectResolvers.ContainsKey(id)) throw new InvalidOperationException("对象解析器已登记：" + id);
            ObjectResolvers.Add(id, resolver);
        }

        public static void UnregisterObjectResolver(string id) { if (id != null) ObjectResolvers.Remove(id); }

        /// <summary>项目解析优先；无匹配才查 Prefab 素材。冲突、失效或异常抛错，禁止降级为空适配器。</summary>
        public static IEmberLevelObjectAdapter ResolveObject(GameObject instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            IEmberLevelObjectAdapter result = null;
            string owner = null;
            // 固定顺序与快照使诊断稳定；解析器不得修改登记或场景。
            foreach (var entry in ObjectResolvers.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray())
            {
                IEmberLevelObjectAdapter candidate;
                try { candidate = entry.Value(instance); }
                catch (Exception error) { throw new InvalidOperationException($"对象解析器 {entry.Key} 失败（{instance.name}）：{error.Message}", error); }
                if (candidate == null) continue;
                if (string.IsNullOrWhiteSpace(candidate.Id) || !ReferenceEquals(GetObject(candidate.Id), candidate))
                    throw new InvalidOperationException($"对象解析器 {entry.Key} 返回失效或未登记的适配器：{candidate.Id}（{instance.name}）。");
                if (result != null && !ReferenceEquals(result, candidate))
                    throw new InvalidOperationException($"对象解析器冲突（{instance.name}）：{owner} → {result.Id}，{entry.Key} → {candidate.Id}。");
                result = candidate; owner = entry.Key;
            }
            if (result != null) return result;
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            if (source == null) return null;
            var materials = EmberLevelAssetService.Find<EmberLevelMaterial>().Where(m => m.Prefab != null && m.Prefab == source).ToArray();
            var ids = materials.Select(m => m.AdapterId ?? "").Distinct(StringComparer.Ordinal).ToArray();
            if (ids.Length > 1) throw new InvalidOperationException("Prefab 素材适配器冲突（" + instance.name + "）：" + string.Join(", ", ids));
            if (ids.Length == 0 || string.IsNullOrEmpty(ids[0])) return null;
            result = GetObject(ids[0]);
            if (result == null) throw new InvalidOperationException("Prefab 素材缺少对象适配器：" + ids[0] + "（" + instance.name + "）。");
            return result;
        }
        #endregion
    }
}
