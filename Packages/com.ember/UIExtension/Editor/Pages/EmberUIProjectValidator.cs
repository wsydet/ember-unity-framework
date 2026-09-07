// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using Ember.Core.Editor;

namespace Ember.UIExtension.Editor
{
    /// <summary>将 UI 开发中心的只读扫描结果接入项目中心。</summary>
    public sealed class EmberUIProjectValidator : IEmberProjectValidator
    {
        #region 外部方法

        public string DisplayName => "UI 生成链路";

        public void Validate(EmberProjectValidationReport report)
        {
            var snapshot = EUIPrefabCatalogService.Scan();
            if (!snapshot.IsConfigured)
            {
                report.Add(EmberValidationSeverity.Warning, DisplayName, snapshot.Error,
                    suggestion: "在 UI 开发中心检查绑定与输出路径配置；本次检查不会自动创建配置。");
                return;
            }
            foreach (var entry in snapshot.Entries)
            {
                if (entry.MissingScriptCount > 0)
                    Add(report, entry, $"存在 {entry.MissingScriptCount} 个 Missing Script。");
                if (entry.NullBindingCount > 0)
                    Add(report, entry, $"存在 {entry.NullBindingCount} 个空引用绑定。");
                if (entry.NoCodeGeneration) continue;
                if (!entry.LogicScriptExists) Add(report, entry, "缺少 UI 逻辑脚本。");
                if (!entry.BindingScriptExists) Add(report, entry, "缺少生成的 Binding 脚本。");
                if (entry.GenerateCustomSettings && !entry.SettingsScriptExists)
                    Add(report, entry, "配置了自定义 Settings，但对应脚本缺失。");
                if (entry.IsPage && (!entry.PageDefOk || entry.PageDefMatchCount != 1))
                    Add(report, entry, "页面注册缺失、重复或 Prefab 路径不匹配。");
            }
            report.Add(EmberValidationSeverity.Information, DisplayName,
                $"已检查 {snapshot.Entries.Count} 个 UI Prefab；Item 不要求 GamePages 注册，不生成代码的 UI 不要求脚本。",
                snapshot.UIResourceRoot, "需要修复时进入 UI 开发中心或打开对应 Prefab。");
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void Add(EmberProjectValidationReport report, EUIPrefabCatalogEntry entry, string message)
        {
            report.Add(EmberValidationSeverity.Error, DisplayName, message, entry.PrefabPath,
                "在 UI 开发中心检查该 UI，确认绑定后重新生成；保留用户逻辑区。");
        }

        #endregion
    }
}
