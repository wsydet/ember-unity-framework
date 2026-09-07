// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ember.Core.Editor
{
    public enum EmberValidationSeverity { Passed, Information, Difference, Warning, Error }

    /// <summary>一次只读检查中的问题、定位对象和处理建议。</summary>
    public sealed class EmberValidationIssue
    {
        public EmberValidationSeverity Severity { get; internal set; }
        public string Category { get; internal set; }
        public string Message { get; internal set; }
        public string AssetPath { get; internal set; }
        public string Suggestion { get; internal set; }
    }

    /// <summary>检查结果保留实际比较基准；差异与错误分开计数。</summary>
    public sealed class EmberProjectValidationReport
    {
        #region 内部参数

        private readonly List<EmberValidationIssue> _issues = new();
        public IReadOnlyList<EmberValidationIssue> Issues => _issues;
        public DateTime CheckedAt { get; } = DateTime.Now;
        public string TemplateId { get; internal set; }
        public string Baseline { get; internal set; }
        public string ProjectFingerprint { get; internal set; }
        public bool ComparisonCompleted { get; internal set; }
        public bool IsEditingTemplate { get; internal set; }
        public int ErrorCount => _issues.Count(item => item.Severity == EmberValidationSeverity.Error);
        public int WarningCount => _issues.Count(item => item.Severity == EmberValidationSeverity.Warning);
        public int DifferenceCount => _issues.Count(item => item.Severity == EmberValidationSeverity.Difference);

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public void Add(EmberValidationSeverity severity, string category, string message,
            string assetPath = null, string suggestion = null)
        {
            _issues.Add(new EmberValidationIssue
            {
                Severity = severity, Category = category, Message = message,
                AssetPath = assetPath, Suggestion = suggestion
            });
        }

        public string ToPlainText()
        {
            var result = new StringBuilder();
            result.AppendLine($"项目校验 · {CheckedAt:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine("基准：" + Baseline);
            result.AppendLine($"错误 {ErrorCount} · 警告 {WarningCount} · 差异 {DifferenceCount}");
            foreach (var issue in _issues)
            {
                result.AppendLine($"[{issue.Severity}] {issue.Category}：{issue.Message}");
                if (!string.IsNullOrEmpty(issue.AssetPath)) result.AppendLine(issue.AssetPath);
                if (!string.IsNullOrEmpty(issue.Suggestion)) result.AppendLine("建议：" + issue.Suggestion);
            }
            return result.ToString();
        }

        #endregion
    }

    /// <summary>Editor 子系统提供只读检查；通过 TypeCache 发现，不让 Core 反向引用 UI 编辑器。</summary>
    public interface IEmberProjectValidator
    {
        string DisplayName { get; }
        void Validate(EmberProjectValidationReport report);
    }
}
