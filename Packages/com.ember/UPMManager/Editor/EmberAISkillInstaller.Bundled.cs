// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    public static partial class EmberAISkillInstaller
    {
        #region 内部参数
        [Serializable] internal sealed class BundleManifest
        {
            public int schemaVersion;
            public string generatedBy;
            public string sourceCommit;
            public Fingerprint[] files;
        }
        #endregion

        #region 外部方法
        /// <summary>框架附带的通用技能使用原安装器。只安装无目录且无历史记录的技能，升级不覆盖已有副本。</summary>
        internal static IReadOnlyList<string> InstallBundledMissing(string projectRoot, string bundleRoot,
            string frameworkVersion, bool hasEuiApi)
        {
            var messages = new List<string>();
            if (IsSourceProject(projectRoot)) return messages;
            string manifestPath = Within(bundleRoot, "bundle.json");
            if (!File.Exists(manifestPath)) return messages; // Old packages did not carry a bundle.
            var manifest = JsonUtility.FromJson<BundleManifest>(File.ReadAllText(manifestPath, Utf8));
            string skills = Within(bundleRoot, "skills");
            if (manifest == null || manifest.schemaVersion != 1 || manifest.generatedBy != "Ember AI Skill Bundle"
                || !System.Text.RegularExpressions.Regex.IsMatch(manifest.sourceCommit ?? "", @"\A[0-9a-f]{40,64}\z")
                || !Same(Snapshot(skills), manifest.files))
                throw new InvalidDataException("框架附带 Skill 清单/指纹无效；请重新生成并发布框架包。");
            var packages = ReadCatalog(skills);
            foreach (var package in packages)
            {
                string id = package.Definition.id;
                var state = ReadState(projectRoot);
                string target = Within(projectRoot, ".agents/skills/" + id);
                if (state.skills.Any(s => s.id == id) || Directory.Exists(target) || File.Exists(target))
                { messages.Add(id + "：保留已有目录/安装记录，可在 UPM Manager 显式检查更新。"); continue; }
                string error = Incompatibility(package.Definition, frameworkVersion, hasEuiApi);
                if (error != null) { messages.Add(id + "：" + error); continue; }
                Install(projectRoot, Inspect(projectRoot, package), "package:com.ember", frameworkVersion,
                    manifest.sourceCommit, false);
                messages.Add(id + "：已安装框架附带技能。");
            }
            return messages.AsReadOnly();
        }
        #endregion
    }
}
