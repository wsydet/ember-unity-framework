// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ember.Core.Editor
{
    public static partial class EmberProjectSetup
    {
        #region 内部参数
        internal sealed class TemplateIdentity
        {
            internal string Id, Version;
            internal readonly HashSet<string> Ancestors = new(StringComparer.Ordinal);
        }
        internal static TemplateIdentity CurrentIdentity => EmberEditorMetadataCache.Get("formal-identity", BuildIdentity);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static TemplateIdentity BuildIdentity()
        {
            var identity = new TemplateIdentity();
            bool embedded = IsEmbeddedPackage();
            var editing = embedded ? GetEditingTemplate() : null;
            var deployed = embedded ? null : GetActiveDeployedTemplate();
            string id = embedded ? editing?.templateId : deployed?.templateId;
            string version = embedded ? editing?.templateVersion : deployed?.version;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version)
                || string.IsNullOrWhiteSpace(embedded ? editing?.contentHash : deployed?.contentHash)) return identity;
            var templates = CachedTemplates;
            if (!IsTemplateActiveForIdentity(id, embedded, editing?.templateId, deployed?.templateId, templates)) return identity;
            identity.Id = id; identity.Version = version;
            while (!string.IsNullOrEmpty(id))
            {
                var template = templates.Single(t => t.id == id);
                if (string.IsNullOrWhiteSpace(template.version) || string.IsNullOrWhiteSpace(template.contentHash))
                    return new TemplateIdentity();
                identity.Ancestors.Add(id);
                id = template.parentId;
            }
            return identity;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法

        /// <summary>当前正式编辑/部署模板是否为指定模板或其派生模板。只读，不回退到另一环境的记录。</summary>
        public static bool IsTemplateActive(string templateId)
        {
            try
            {
                return templateId != null && CurrentIdentity.Ancestors.Contains(templateId);
            }
            catch (Exception) { return false; }
        }

        internal static bool IsTemplateActiveForIdentity(string templateId, bool embedded,
            string editingId, string deployedId, IReadOnlyList<TemplateInfo> templates)
        {
            if (string.IsNullOrEmpty(templateId) || templates == null) return false;
            string id = embedded ? editingId : deployedId;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            bool matches = false;
            while (!string.IsNullOrEmpty(id) && visited.Add(id))
            {
                TemplateInfo current = null;
                foreach (var candidate in templates)
                {
                    if (candidate == null || !string.Equals(candidate.id, id, StringComparison.Ordinal)) continue;
                    if (current != null) return false;
                    current = candidate;
                }
                if (current == null) return false;
                if (string.Equals(id, templateId, StringComparison.Ordinal)) matches = true;
                id = current.parentId;
            }
            // 命中当前节点后仍校验到根，不能授权断链或循环中的模板。
            return string.IsNullOrEmpty(id) && matches;
        }

        #endregion
    }
}
