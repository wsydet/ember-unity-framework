using System;
using System.Collections.Generic;
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
        #endregion
    }
}
