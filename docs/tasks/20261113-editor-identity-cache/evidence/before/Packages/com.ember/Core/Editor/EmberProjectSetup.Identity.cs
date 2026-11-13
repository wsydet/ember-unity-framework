// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

namespace Ember.Core.Editor
{
    public static partial class EmberProjectSetup
    {
        #region 外部方法

        /// <summary>当前正式编辑/部署模板是否为指定模板或其派生模板。只读，不回退到另一环境的记录。</summary>
        public static bool IsTemplateActive(string templateId)
        {
            try
            {
                bool embedded = IsEmbeddedPackage();
                return IsTemplateActiveForIdentity(templateId, embedded,
                    embedded ? GetEditingTemplate()?.templateId : null,
                    embedded ? null : GetActiveDeployedTemplate()?.templateId, GetTemplates());
            }
            catch (IOException) { return false; }
        }

        internal static bool IsTemplateActiveForIdentity(string templateId, bool embedded,
            string editingId, string deployedId, IReadOnlyList<TemplateInfo> templates)
        {
            if (string.IsNullOrEmpty(templateId) || templates == null) return false;
            string id = embedded ? editingId : deployedId;
            var visited = new HashSet<string>(StringComparer.Ordinal);
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
                if (string.Equals(id, templateId, StringComparison.Ordinal)) return true;
                id = current.parentId;
            }
            return false;
        }

        #endregion
    }
}
