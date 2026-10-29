using System;
using System.Collections.Generic;
using System.Linq;

namespace Ember.Core.Editor
{
    public enum EmberExtensionKind { Infrastructure, RuntimeCapability, EditorExtension }

    /// <summary>声明组合关系；不构造运行宿主，也不改变 Manager/Module 生命周期。</summary>
    public sealed class EmberExtensionDefinition
    {
        #region 内部参数
        public string Id { get; }
        public string DisplayName { get; }
        public EmberExtensionKind Kind { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public string Compatibility { get; }
        public int ContractVersion { get; }
        public IReadOnlyList<string> SourceRoots { get; }
        internal Func<string> CheckCompatibility { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberExtensionDefinition(string id, string name, EmberExtensionKind kind, IEnumerable<string> dependencies = null,
            string compatibility = "Unity 6 / com.ember 单包", Func<string> checkCompatibility = null,
            int contractVersion = 1, IEnumerable<string> sourceRoots = null)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Any(c => !(char.IsLower(c) || char.IsDigit(c) || c == '.' || c == '-')))
                throw new ArgumentException("扩展 ID 只能包含小写字母、数字、点和短横线。", nameof(id));
            Id = id; DisplayName = name; Kind = kind;
            Dependencies = Array.AsReadOnly((dependencies ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray());
            Compatibility = compatibility; CheckCompatibility = checkCompatibility;
            if (contractVersion < 1) throw new ArgumentOutOfRangeException(nameof(contractVersion));
            ContractVersion = contractVersion;
            SourceRoots = Array.AsReadOnly((sourceRoots ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        #endregion
    }

    public sealed class EmberExtensionResolution
    {
        #region 内部参数
        private readonly HashSet<string> _enabled;
        public IReadOnlyDictionary<string, string> Problems { get; }
        public IReadOnlyList<string> Enabled { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberExtensionResolution(HashSet<string> enabled, Dictionary<string, string> problems)
        { _enabled = enabled; Enabled = enabled.OrderBy(x => x, StringComparer.Ordinal).ToArray(); Problems = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(problems); }
        public bool IsEnabled(string id) => id != null && _enabled.Contains(id);
        #endregion
    }

    /// <summary>纯依赖求解器。缺失、循环、兼容条件失败只阻止受影响分支，不隐式删除未知配置。</summary>
    public static class EmberExtensionCatalog
    {
        #region 内部方法
        private static bool Visit(string id, IReadOnlyDictionary<string, EmberExtensionDefinition> catalog,
            HashSet<string> visiting, HashSet<string> enabled, Dictionary<string, string> problems)
        {
            if (enabled.Contains(id)) return true;
            if (problems.ContainsKey(id)) return false;
            if (!catalog.TryGetValue(id, out var definition)) { problems[id] = "扩展未登记：" + id; return false; }
            if (!visiting.Add(id)) { problems[id] = "循环依赖：" + string.Join(" → ", visiting) + " → " + id; return false; }
            string reason;
            try { reason = definition.CheckCompatibility?.Invoke(); }
            catch (Exception error) { reason = "兼容检查失败：" + error.Message; }
            bool valid = string.IsNullOrEmpty(reason);
            if (!valid) problems[id] = reason;
            foreach (var dependency in definition.Dependencies)
                if (!Visit(dependency, catalog, visiting, enabled, problems))
                { valid = false; problems[id] = "依赖不可用：" + dependency + "（" + problems[dependency] + "）"; }
            visiting.Remove(id);
            if (valid) enabled.Add(id);
            return valid;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static EmberExtensionResolution Resolve(IEnumerable<EmberExtensionDefinition> definitions, IEnumerable<string> requested)
        {
            var catalog = definitions.ToDictionary(d => d.Id, StringComparer.Ordinal);
            var enabled = new HashSet<string>(StringComparer.Ordinal);
            var problems = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string id in (requested ?? Array.Empty<string>()).Concat(catalog.Values.Where(d => d.Kind == EmberExtensionKind.Infrastructure).Select(d => d.Id)).Distinct().OrderBy(x => x, StringComparer.Ordinal))
                Visit(id ?? "", catalog, new HashSet<string>(StringComparer.Ordinal), enabled, problems);
            return new EmberExtensionResolution(enabled, problems);
        }

        /// <summary>关闭一个依赖时，要从显式组合中移除哪些根；不执行保存。</summary>
        public static string[] PreviewDisable(IEnumerable<EmberExtensionDefinition> definitions, IEnumerable<string> requested, string id)
        {
            var catalog = definitions.ToDictionary(d => d.Id, StringComparer.Ordinal);
            bool Depends(string current, HashSet<string> visited) => current == id || visited.Add(current) && catalog.TryGetValue(current, out var d) && d.Dependencies.Any(x => Depends(x, visited));
            return requested.Where(root => Depends(root, new HashSet<string>(StringComparer.Ordinal))).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }
        #endregion
    }
}
