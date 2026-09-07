// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ember.Basic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Ember 项目脚手架向导 —— 从包内 Templates~ 模板整树部署业务演示到 Assets/。
    ///
    /// 模板体系（见 docs/dev/upm-migration-plan.md §6.7）：
    /// 框架交付的就是"演示形态"——继承、绑定、场景对象全部替用户做好，
    /// 用户只在状态钩子函数里填自己的代码（类 Unity Mono 生命周期）。
    ///
    /// 部署 = 整树复制（.meta 随行，GUID 全链有效）；幂等：已存在文件跳过（用户改动不覆盖）。
    /// </summary>
    public static class EmberProjectSetup
    {
        #region 内部参数

        private const string TAG = LogTags.CoreEditor;
        private const string PACKAGE = "com.ember";

        private const string ScenesDir = "Assets/Game/Scenes";
        private const string FrameworkScenePath = ScenesDir + "/FrameworkScene.unity";
        private const string MainScenePath = ScenesDir + "/MainScene.unity";
        private const string GameplayScenePath = ScenesDir + "/GameplayScene.unity";
        private const string SettingsScenePath = ScenesDir + "/SettingsScene.unity";
        private const string SceneMappingPath = "Assets/Ember/Editor/SOs/EmberSceneMapping.asset";

        /// <summary>新建模板的初始版本（模板版本独立于框架版本，从 0.1.0 起）。</summary>
        private const string InitialTemplateVersion = "0.1.0";

        /// <summary>消费端部署记录（项目级状态，位于被镜像的 Assets/Ember/Editor/ 之外，不属于模板内容）。</summary>
        private const string DeployedRecordsPath = "Assets/Editor/EmberDeployedTemplates.json";

        /// <summary>dev 仓库「当前正在编辑的模板」记录（项目级状态，位于被镜像目录之外）。</summary>
        private const string EditingRecordPath = "Assets/Editor/EmberEditingTemplate.json";

        private const string ChannelStable = "stable";
        private const string ChannelPreview = "preview";
        private const string ChannelDeprecated = "deprecated";

        /// <summary>模板快照覆盖的业务层目录（相对项目 Assets/）。GameResource = UI 预制体等资源区（与代码目录分离，资源加载友好）。</summary>
        private static readonly string[] TemplateDirNames = { "Game", "Resources", "Ember/Editor", "Settings", "GameResource" };

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>执行完整初始化：部署指定模板 + 注册场景 + 刷新场景映射。返回部署的文件数。</summary>
        public static int Initialize(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return 0;

            var deploymentData = ReadDeploymentData();
            var blockReason = GetDeploymentBlockReason(deploymentData, templateId);
            if (!string.IsNullOrEmpty(blockReason))
                throw new InvalidOperationException(blockReason);

            int deployed = DeployTemplate(packagePath, templateId);
            RegisterBuildSettings();
            EmberSceneMappingCreator.EnsureAndRescan();
            RecordDeployment(packagePath, templateId);
            AssetDatabase.Refresh();
            return deployed;
        }

        /// <summary>扫描包内 Templates~/ 下的所有模板（未来新增模板自动出现在列表）。</summary>
        public static List<TemplateInfo> GetTemplates()
        {
            var result = new List<TemplateInfo>();
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return result;

            var root = Path.Combine(packagePath, "Templates~");
            if (!Directory.Exists(root)) return result;

            foreach (var dir in Directory.GetDirectories(root))
            {
                var jsonPath = Path.Combine(dir, "template.json");
                if (!File.Exists(jsonPath)) continue;

                var info = ReadTemplateJson(packagePath, Path.GetFileName(dir));
                if (info != null && !string.IsNullOrEmpty(info.id))
                    result.Add(info);
            }

            return result.OrderBy(t => t.order).ToList();
        }

        /// <summary>计算指定模板 Assets 目录的确定性完整树 hash（包含 .meta）。</summary>
        public static string ComputeTemplateContentHash(string assetsPath)
        {
            return EmberTemplateInheritanceEngine.ComputeTemplateContentHash(assetsPath);
        }

        /// <summary>构建 schema v1 → v2 的只读迁移计划；不会修改模板内容或 template.json。</summary>
        public static TemplateMetadataMigrationPlan GetTemplateMetadataMigrationPlan(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null)
                throw new InvalidOperationException($"包 [{PACKAGE}] 未安装。");

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在或元数据损坏。");
            var assetsPath = Path.Combine(packagePath, "Templates~", templateId, "Assets");
            return EmberTemplateInheritanceEngine.BuildMetadataMigrationPlan(info, assetsPath);
        }

        /// <summary>
        /// 显式初始化模板继承元数据：只把 schema v1 metadata 升级为 v2 并写入实时 hash，
        /// 不修改模板 Assets 或模板版本。重复调用保持幂等。
        /// </summary>
        public static TemplateInfo InitializeTemplateInheritanceMetadata(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null)
                throw new InvalidOperationException($"包 [{PACKAGE}] 未安装。");

            var plan = GetTemplateMetadataMigrationPlan(templateId);
            if (!string.IsNullOrEmpty(plan.Error))
                throw new InvalidOperationException(plan.Error);
            if (!plan.IsRequired)
                return EmberTemplateInheritanceEngine.Clone(plan.Source);
            if (!plan.CanApply || plan.Migrated == null)
                throw new InvalidOperationException($"模板 [{templateId}] 的继承元数据无法安全初始化。");

            WriteTemplateJson(packagePath, plan.Migrated);
            EmberDebug.LogInit(TAG,
                $"模板 [{templateId}] 已初始化 schema v{EmberTemplateInheritanceEngine.CurrentSchemaVersion} 继承元数据，内容与版本保持不变。");
            return EmberTemplateInheritanceEngine.Clone(plan.Migrated);
        }

        /// <summary>校验包内全部模板的父级谱系；只读，不修改 metadata。</summary>
        public static List<TemplateGraphIssue> ValidateTemplateGraph()
        {
            return EmberTemplateInheritanceEngine.ValidateTemplateGraph(GetTemplates());
        }

        /// <summary>计算父模板 → 派生模板的只读三方同步计划。</summary>
        public static TemplateSyncPlan ComputeParentSyncPlan(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null)
                throw new InvalidOperationException($"包 [{PACKAGE}] 未安装。");

            var templates = GetTemplates();
            var child = templates.Find(item => item.id == templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在或元数据损坏。");
            var templateRoot = Path.Combine(packagePath, "Templates~", templateId);
            var parentAssets = string.IsNullOrEmpty(child.parentId)
                ? null
                : Path.Combine(packagePath, "Templates~", child.parentId, "Assets");
            return EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                child,
                templates,
                parentAssets,
                Path.Combine(templateRoot, "ParentSnapshot~", "Assets"),
                Path.Combine(templateRoot, "Assets"));
        }

        /// <summary>
        /// 显式 dry-run：先构建纯文件级 O/N/C 计划，再仅对合格的 .unity 并发修改尝试语义增强。
        /// 普通状态刷新必须继续调用 ComputeParentSyncPlan，避免启动外部进程。
        /// </summary>
        public static TemplateSyncPlan ComputeParentSyncPreviewPlan(string templateId)
        {
            var filePlan = ComputeParentSyncPlan(templateId);
            if (filePlan.Status != TemplateSyncStatus.ParentChanged
                || !filePlan.IsEligible
                || filePlan.Changes.Count == 0)
                return filePlan;

            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null)
                throw new InvalidOperationException($"包 [{PACKAGE}] 未安装。");

            var childRoot = Path.Combine(packagePath, "Templates~", templateId);
            return EmberTemplateSceneMergePlanner.EnhancePlan(
                filePlan,
                Path.Combine(childRoot, "ParentSnapshot~", "Assets"),
                Path.Combine(packagePath, "Templates~", filePlan.ParentId, "Assets"),
                Path.Combine(childRoot, "Assets"),
                new EmberUnityYamlMerge());
        }

        /// <summary>读取模板当前继承同步状态。</summary>
        public static TemplateSyncStatus GetTemplateSyncStatus(string templateId)
        {
            var plan = ComputeParentSyncPlan(templateId);
            var editing = GetEditingTemplate();
            if (editing == null
                || !string.Equals(editing.templateId, templateId, StringComparison.Ordinal))
                return plan.Status;

            var template = GetTemplates().Find(item => item.id == templateId);
            return EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, template)
                ? TemplateSyncStatus.EditingCopyStale
                : plan.Status;
        }

        /// <summary>当前项目编辑副本是否已落后于对应模板存储。</summary>
        public static bool IsEditingCopyStale(string templateId)
        {
            var editing = GetEditingTemplate();
            if (editing == null
                || !string.Equals(editing.templateId, templateId, StringComparison.Ordinal))
                return false;

            var template = GetTemplates().Find(item => item.id == templateId);
            return EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, template);
        }

        /// <summary>应用已预览的父模板三方同步计划，并原子更新派生 Assets、基线和 metadata。</summary>
        public static TemplateInfo ApplyParentSync(
            TemplateSyncPlan plan,
            IReadOnlyDictionary<string, TemplateConflictChoice> resolutions,
            int? versionBumpField)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null)
                throw new InvalidOperationException($"包 [{PACKAGE}] 未安装。");

            var currentPlan = ComputeParentSyncPlan(plan.TemplateId);
            EnsurePlanIsCurrent(plan, currentPlan);

            var templates = GetTemplates();
            var child = templates.Find(item => item.id == plan.TemplateId)
                ?? throw new InvalidOperationException($"模板 [{plan.TemplateId}] 不存在。");
            var parent = templates.Find(item => item.id == child.parentId)
                ?? throw new InvalidOperationException($"父模板 [{child.parentId}] 不存在。");

            var editing = GetEditingTemplate();
            if (editing != null
                && string.Equals(editing.templateId, child.id, StringComparison.Ordinal)
                && EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, child))
            {
                throw new InvalidOperationException(
                    $"模板 [{child.id}] 的项目编辑副本已过期；请先重新加载，不能继续父模板同步。");
            }

            var childRoot = Path.Combine(packagePath, "Templates~", child.id);
            var parentRoot = Path.Combine(packagePath, "Templates~", parent.id);
            var updated = EmberTemplateTransaction.ApplyParentSync(
                new ParentSyncTransactionRequest(
                    plan,
                    child,
                    parent,
                    childRoot,
                    Path.Combine(childRoot, "Assets"),
                    Path.Combine(parentRoot, "Assets"),
                    Path.Combine(childRoot, "ParentSnapshot~", "Assets"),
                    resolutions,
                    versionBumpField));

            EmberDebug.LogInit(TAG,
                $"模板 [{child.id}] 已同步父模板 [{parent.id}]，当前版本 v{updated.version}。");
            return EmberTemplateInheritanceEngine.Clone(updated);
        }

        /// <summary>模板声明的场景列表（Assets-relative 路径，扫描 Templates~/{id}/Assets/Game/Scenes/*.unity，升序）。供初始化窗口按模板展示场景注册状态。</summary>
        public static List<string> GetTemplateScenes(string templateId)
        {
            var result = new List<string>();
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return result;

            var scenesDir = Path.Combine(packagePath, "Templates~", templateId, "Assets", "Game", "Scenes");
            if (!Directory.Exists(scenesDir)) return result;

            foreach (var file in Directory.GetFiles(scenesDir, "*.unity"))
                result.Add("Assets/Game/Scenes/" + Path.GetFileName(file));
            result.Sort();
            return result;
        }

        /// <summary>当前框架包版本（如 "0.8.0"）。</summary>
        public static string GetFrameworkVersion()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE);
            return info?.version ?? "0.0.0";
        }

        /// <summary>兼容闸门：模板声明的框架版本与当前框架 major.minor 一致。</summary>
        public static bool IsFrameworkCompatible(string templateFrameworkVersion)
        {
            var v = ParseTemplateVersion(templateFrameworkVersion);
            var fw = ParseTemplateVersion(GetFrameworkVersion());
            return v[0] == fw[0] && v[1] == fw[1];
        }

        /// <summary>按兼容闸门过滤模板（channel=deprecated 视为隐藏；preview 保留，由 UI 加徽标）。</summary>
        public static List<TemplateInfo> GetCompatibleTemplates()
        {
            return GetTemplates()
                .Where(t => t.channel != ChannelDeprecated && IsFrameworkCompatible(t.frameworkVersion))
                .ToList();
        }

        /// <summary>模板升级等级：已部署版本 → 包内版本（major=弃用重写 / minor=结构变化 / patch=修复）。</summary>
        public static TemplateUpgradeLevel GetTemplateUpgradeLevel(string deployedVersion, string currentVersion)
        {
            var d = ParseTemplateVersion(deployedVersion);
            var c = ParseTemplateVersion(currentVersion);
            if (d[0] != c[0]) return TemplateUpgradeLevel.Major;
            if (d[1] != c[1]) return TemplateUpgradeLevel.Minor;
            if (d[2] != c[2]) return TemplateUpgradeLevel.Patch;
            return TemplateUpgradeLevel.None;
        }

        /// <summary>判断模板是否已部署（按关键标记文件）。</summary>
        public static bool IsTemplateDeployed(string templateId)
        {
            var active = ResolveActiveDeployment(ReadDeploymentData());
            return active != null
                && string.Equals(active.templateId, templateId, StringComparison.Ordinal);
        }

        /// <summary>兼容旧入口：fromBase=true 创建 base 的物化派生模板，否则创建独立模板。</summary>
        public static int CreateTemplate(string templateId, string displayName, string description, bool fromBase)
        {
            return CreateTemplate(
                templateId,
                displayName,
                description,
                fromBase ? "base" : null);
        }

        /// <summary>新建独立模板或指定父模板的完整物化派生模板；返回模板 Assets 文件数。</summary>
        public static int CreateTemplate(
            string templateId,
            string displayName,
            string description,
            string parentId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return -1;

            ValidateNewTemplateId(templateId);
            var templates = GetTemplates();
            if (templates.Any(item =>
                    string.Equals(item.id, templateId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"模板 [{templateId}] 已存在，请换一个 id。");

            var tplRoot = Path.Combine(packagePath, "Templates~", templateId);
            if (Directory.Exists(tplRoot))
                throw new InvalidOperationException($"模板 [{templateId}] 已存在，请换一个 id。");

            TemplateInfo parent = null;
            if (!string.IsNullOrEmpty(parentId))
            {
                parent = templates.Find(item => item.id == parentId)
                    ?? throw new InvalidOperationException($"父模板 [{parentId}] 不存在。");
                EnsureParentCanBeBranched(packagePath, parent, templates);
            }

            var templatesRoot = Path.Combine(packagePath, "Templates~");
            var stageRoot = Path.Combine(
                templatesRoot,
                $".{templateId}.create-staging-{Guid.NewGuid():N}~");
            int n = 0;
            try
            {
                var stagedAssets = Path.Combine(stageRoot, "Assets");
                if (parent == null)
                {
                    Directory.CreateDirectory(stagedAssets);
                }
                else
                {
                    var parentAssets = Path.Combine(
                        templatesRoot,
                        parent.id,
                        "Assets");
                    n = EmberTemplateTransaction.CopyDirectory(parentAssets, stagedAssets);
                    EmberTemplateTransaction.CopyDirectory(
                        parentAssets,
                        Path.Combine(stageRoot, "ParentSnapshot~", "Assets"));
                }

                if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                        stagedAssets,
                        out var contentHash,
                        out var validationError))
                {
                    throw new InvalidOperationException(
                        $"新模板内容未通过资源/.meta/GUID 校验：{validationError}");
                }
                if (parent != null)
                {
                    if (!string.Equals(contentHash, parent.contentHash, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"父模板 [{parent.id}] 在派生创建期间发生变化，请重新创建。");
                    }
                    var stagedSnapshot = Path.Combine(stageRoot, "ParentSnapshot~", "Assets");
                    if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                            stagedSnapshot,
                            out var snapshotHash,
                            out var snapshotError)
                        || !string.Equals(snapshotHash, parent.contentHash, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"派生模板基线未能完整复制父模板：{snapshotError ?? "内容 hash 不一致"}");
                    }
                }

                var info = new TemplateInfo
                {
                    schemaVersion = EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                    id = templateId,
                    displayName = string.IsNullOrEmpty(displayName) ? templateId : displayName,
                    description = description ?? "",
                    version = InitialTemplateVersion,
                    frameworkVersion = parent?.frameworkVersion ?? GetFrameworkVersion(),
                    channel = parent == null ? ChannelStable : ChannelPreview,
                    order = 99,
                    parentId = parent?.id ?? string.Empty,
                    parentVersion = parent?.version ?? string.Empty,
                    parentContentHash = parent?.contentHash ?? string.Empty,
                    contentHash = contentHash,
                    versionedContentHash = contentHash
                };
                EmberTemplateTransaction.WriteTemplateJson(
                    Path.Combine(stageRoot, "template.json"),
                    info);
                EmberTemplateTransaction.CommitPreparedTargets(
                    new[] { new TemplateTransactionTarget(stageRoot, tplRoot) });
            }
            finally
            {
                TryCleanTransactionPath(stageRoot);
            }

            EmberDebug.LogInit(TAG,
                $"模板 [{templateId}] 已创建（{(parent == null ? "独立空模板" : $"派生自 {parent.id}，物化 {n} 文件")}）。");
            return n;
        }

        /// <summary>
        /// 把当前、未过期的编辑副本另存为新的物化派生模板。
        /// 当前编辑模板成为父模板；新模板与编辑记录作为一个事务提交。
        /// </summary>
        public static int SaveEditingCopyAsNewTemplate(
            string templateId,
            string displayName,
            string description)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (packagePath == null || projectRoot == null) return -1;

            ValidateNewTemplateId(templateId);
            var templates = GetTemplates();
            if (templates.Any(item => string.Equals(
                    item.id,
                    templateId,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"模板 [{templateId}] 已存在，请换一个 id。");

            var editing = GetEditingTemplate()
                ?? throw new InvalidOperationException("当前没有可另存为的模板编辑副本，请先加载一个模板。");
            var parent = templates.Find(item => item.id == editing.templateId)
                ?? throw new InvalidOperationException($"当前编辑模板 [{editing.templateId}] 不存在。");
            if (EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, parent))
            {
                throw new InvalidOperationException(
                    $"当前编辑副本已落后于模板 [{parent.id}]，不能安全建立父基线；请先重新加载。");
            }
            EnsureParentCanBeBranched(packagePath, parent, templates);

            var templatesRoot = Path.Combine(packagePath, "Templates~");
            var templateRoot = Path.Combine(templatesRoot, templateId);
            if (Directory.Exists(templateRoot))
                throw new InvalidOperationException($"模板 [{templateId}] 已存在，请换一个 id。");

            var stageRoot = Path.Combine(
                templatesRoot,
                $".{templateId}.save-as-staging-{Guid.NewGuid():N}~");
            var stagedTemplate = Path.Combine(stageRoot, "Template");
            var stagedAssets = Path.Combine(stagedTemplate, "Assets");
            var stagedSnapshot = Path.Combine(stagedTemplate, "ParentSnapshot~", "Assets");
            var stagedEditing = Path.Combine(stageRoot, "EmberEditingTemplate.json");
            try
            {
                int count = CopyProjectBusinessLayer(projectRoot, stagedAssets);
                if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                        stagedAssets,
                        out var contentHash,
                        out var validationError))
                {
                    throw new InvalidOperationException(
                        $"另存为结果未通过资源/.meta/GUID 校验：{validationError}");
                }

                var parentAssets = Path.Combine(templatesRoot, parent.id, "Assets");
                EmberTemplateTransaction.CopyDirectory(parentAssets, stagedSnapshot);
                if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                        stagedSnapshot,
                        out var snapshotHash,
                        out var snapshotError)
                    || !string.Equals(snapshotHash, parent.contentHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"新模板父基线复制失败：{snapshotError ?? "内容 hash 不一致"}");
                }

                var info = new TemplateInfo
                {
                    schemaVersion = EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                    id = templateId,
                    displayName = string.IsNullOrEmpty(displayName) ? templateId : displayName,
                    description = description ?? string.Empty,
                    version = InitialTemplateVersion,
                    frameworkVersion = parent.frameworkVersion,
                    channel = ChannelPreview,
                    order = 99,
                    parentId = parent.id,
                    parentVersion = parent.version,
                    parentContentHash = parent.contentHash,
                    contentHash = contentHash,
                    versionedContentHash = contentHash
                };
                EmberTemplateTransaction.WriteTemplateJson(
                    Path.Combine(stagedTemplate, "template.json"),
                    info);
                EmberTemplateTransaction.WriteJson(
                    stagedEditing,
                    CreateEditingRecord(info));
                EmberTemplateTransaction.CommitPreparedTargets(
                    new[]
                    {
                        new TemplateTransactionTarget(stagedTemplate, templateRoot),
                        new TemplateTransactionTarget(
                            stagedEditing,
                            ToFullPath(EditingRecordPath))
                    });

                EmberDebug.LogInit(TAG,
                    $"当前编辑副本已另存为派生模板 [{templateId}]（{count} 文件）。");
                return count;
            }
            finally
            {
                TryCleanTransactionPath(stageRoot);
            }
        }

        /// <summary>是否为框架开发仓库（embedded 安装）——模板保存/加载仅在此模式可用。</summary>
        public static bool IsEmbeddedPackage()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE);
            return info != null && info.source == UnityEditor.PackageManager.PackageSource.Embedded;
        }

        /// <summary>把当前项目业务层保存为模板（覆盖该模板旧内容，并剥离 dev 测试对象）。返回复制文件数。</summary>
        public static int SaveTemplate(string templateId, string displayName, string description)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (packagePath == null || projectRoot == null) return -1;

            var tplRoot = Path.Combine(packagePath, "Templates~", templateId);
            var tplAssets = Path.Combine(tplRoot, "Assets");
            var existing = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在或元数据损坏。");
            EnsureEditingCopyCanSave(existing);
            EnsureStoredContentMatchesMetadata(existing, tplAssets, "保存");

            var templatesRoot = Path.Combine(packagePath, "Templates~");
            var stageRoot = Path.Combine(
                templatesRoot,
                $".{templateId}.save-staging-{Guid.NewGuid():N}~");
            var stagedAssets = Path.Combine(stageRoot, "Assets");
            var stagedMetadata = Path.Combine(stageRoot, "template.json");
            var stagedEditingRecord = Path.Combine(stageRoot, "EmberEditingTemplate.json");
            int n = 0;
            try
            {
                n = CopyProjectBusinessLayer(projectRoot, stagedAssets);

                if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                        stagedAssets,
                        out var contentHash,
                        out var validationError))
                {
                    throw new InvalidOperationException(
                        $"保存结果未通过资源/.meta/GUID 校验：{validationError}");
                }

                // 普通保存只更新实时内容 hash；版本封存由 Bump/SetTemplateVersion 显式完成，
                // frameworkVersion 也只由根模板显式声明或派生模板同步继承。
                var info = EmberTemplateInheritanceEngine.BuildSavedMetadata(
                    existing,
                    displayName,
                    description,
                    contentHash);
                var editing = CreateEditingRecord(info);
                EmberTemplateTransaction.WriteTemplateJson(stagedMetadata, info);
                EmberTemplateTransaction.WriteJson(stagedEditingRecord, editing);
                EmberTemplateTransaction.CommitPreparedTargets(
                    new[]
                    {
                        new TemplateTransactionTarget(stagedAssets, tplAssets),
                        new TemplateTransactionTarget(
                            stagedMetadata,
                            Path.Combine(tplRoot, "template.json")),
                        new TemplateTransactionTarget(
                            stagedEditingRecord,
                            ToFullPath(EditingRecordPath))
                    });

                EmberDebug.LogInit(TAG, $"模板 [{templateId}] 已保存（{n} 文件）。");
                return n;
            }
            finally
            {
                TryCleanTransactionPath(stageRoot);
            }
        }

        /// <summary>把模板内容加载到项目业务层（替换当前 Game/Resources/Ember/Editor/Settings，供编辑）。返回复制文件数。</summary>
        public static int LoadTemplate(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (packagePath == null || projectRoot == null) return -1;

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在或元数据损坏。");
            var tplAssets = Path.Combine(packagePath, "Templates~", templateId, "Assets");
            if (!Directory.Exists(tplAssets)) return -1;
            EnsureStoredContentMatchesMetadata(info, tplAssets, "加载");

            var stageRoot = Path.Combine(
                projectRoot,
                "Temp",
                $"EmberTemplateStage-{Guid.NewGuid():N}");
            var stagedEditingRecord = Path.Combine(stageRoot, "EmberEditingTemplate.json");
            int n = 0;

            // 对项目 Assets 的全部落盘操作保持在同一个禁止自动刷新的事务窗口内，
            // 避免 Unity 在真实 .meta 到达前生成随机 GUID。
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                Directory.CreateDirectory(stageRoot);
                var targets = new List<TemplateTransactionTarget>();
                foreach (var rel in TemplateDirNames)
                {
                    var normalizedRel = rel.Replace('/', Path.DirectorySeparatorChar);
                    var src = Path.Combine(tplAssets, normalizedRel);
                    var staged = Path.Combine(stageRoot, normalizedRel);
                    var destination = Path.Combine(projectRoot, "Assets", normalizedRel);
                    if (Directory.Exists(src))
                    {
                        n += EmberTemplateTransaction.CopyDirectory(src, staged);
                    }
                    targets.Add(new TemplateTransactionTarget(
                        staged,
                        destination,
                        allowMissingStage: true));
                }
                EmberTemplateTransaction.WriteJson(
                    stagedEditingRecord,
                    CreateEditingRecord(info));
                targets.Add(new TemplateTransactionTarget(
                    stagedEditingRecord,
                    ToFullPath(EditingRecordPath)));
                EnsureStoredContentMatchesMetadata(info, tplAssets, "加载");
                EmberTemplateTransaction.CommitPreparedTargets(targets);
            }
            finally
            {
                TryCleanTransactionPath(stageRoot);
                AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 已加载到项目业务层（{n} 文件）。");
            return n;
        }

        /// <summary>
        /// 模板版本号 +1（独立于框架版本）。field：0=主版本（次/补丁归零），1=次版本（补丁归零），2=补丁。
        /// 模板不存在时抛异常。
        /// </summary>
        public static void BumpTemplateVersion(string templateId, int field)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return;

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在。");
            EnsureStoredContentMatchesMetadata(
                info,
                Path.Combine(packagePath, "Templates~", templateId, "Assets"),
                "更新版本");

            var original = info;
            info = EmberTemplateInheritanceEngine.BuildBumpedMetadata(info, field);
            CommitVersionMetadata(packagePath, original, info);
            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 版本已更新为 v{info.version}。");
        }

        /// <summary>把模板版本设为指定值（用于误操作回退/人工对齐，格式 x.y.z）。</summary>
        public static void SetTemplateVersion(string templateId, string version)
        {
            if (!Regex.IsMatch(version ?? "", @"^\d+\.\d+\.\d+$"))
                throw new ArgumentException($"版本格式非法 [{version}]，应为 x.y.z（如 0.5.0）。");

            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return;

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在。");
            EnsureStoredContentMatchesMetadata(
                info,
                Path.Combine(packagePath, "Templates~", templateId, "Assets"),
                "设置版本");

            var original = info;
            info = EmberTemplateInheritanceEngine.BuildVersionedMetadata(info, version);
            CommitVersionMetadata(packagePath, original, info);
            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 版本已设为 v{version}。");
        }

        /// <summary>更新模板显示名称/描述（不动版本、排序与模板内容）。</summary>
        public static void UpdateTemplateMetadata(string templateId, string displayName, string description)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return;

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在。");

            info.displayName = string.IsNullOrEmpty(displayName) ? templateId : displayName;
            info.description = description ?? "";
            WriteTemplateJson(packagePath, info);
            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 元数据已更新。");
        }

        /// <summary>删除模板（连同 Templates~/ 下全部内容），不可恢复。</summary>
        public static void DeleteTemplate(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return;

            var tplRoot = Path.Combine(packagePath, "Templates~", templateId);
            if (!Directory.Exists(tplRoot))
                throw new InvalidOperationException($"模板 [{templateId}] 不存在。");

            var child = GetTemplates().Find(item =>
                string.Equals(item.parentId, templateId, StringComparison.Ordinal));
            if (child != null)
            {
                throw new InvalidOperationException(
                    $"模板 [{templateId}] 仍被派生模板 [{child.id}] 引用，不能删除。");
            }
            Directory.Delete(tplRoot, true);

            var editing = GetEditingTemplate();
            if (editing != null && editing.templateId == templateId)
                ClearEditingRecord();

            EmberDebug.LogCleanup(TAG, $"模板 [{templateId}] 已删除。");
        }

        /// <summary>重新声明模板兼容的框架版本为当前框架版本（模板自身版本不变）。</summary>
        public static void DeclareFrameworkVersion(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return;

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在。");
            info = EmberTemplateInheritanceEngine.BuildFrameworkDeclaration(
                info,
                GetFrameworkVersion());
            WriteTemplateJson(packagePath, info);
            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 已声明兼容框架 v{info.frameworkVersion}。");
        }

        /// <summary>设置模板稳定频道：stable / preview / deprecated（deprecated 在消费端隐藏）。</summary>
        public static void SetTemplateChannel(string templateId, string channel)
        {
            if (channel != ChannelStable && channel != ChannelPreview && channel != ChannelDeprecated)
                throw new ArgumentException($"非法 channel [{channel}]，仅支持 stable/preview/deprecated。");

            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return;

            var info = ReadTemplateJson(packagePath, templateId)
                ?? throw new InvalidOperationException($"模板 [{templateId}] 不存在。");

            info.channel = channel;
            WriteTemplateJson(packagePath, info);
            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 频道已设为 {channel}。");
        }

        /// <summary>读取消费端已部署记录（模板从未部署或记录损坏返回 null）。</summary>
        public static DeployedTemplateRecord GetDeployedTemplate(string templateId)
        {
            return CloneDeploymentRecord(ReadDeploymentData().records.Find(record =>
                string.Equals(record.templateId, templateId, StringComparison.Ordinal)));
        }

        /// <summary>读取全部部署历史记录；返回副本，不修改旧格式文件。</summary>
        public static List<DeployedTemplateRecord> GetDeployedTemplates()
        {
            return ReadDeploymentData().records
                .Select(CloneDeploymentRecord)
                .ToList();
        }

        /// <summary>读取当前活动模板；旧格式仅有一条记录时只在内存中兼容推断，不写盘。</summary>
        public static DeployedTemplateRecord GetActiveDeployedTemplate()
        {
            return CloneDeploymentRecord(ResolveActiveDeployment(ReadDeploymentData()));
        }

        /// <summary>旧部署记录有多个候选但没有 activeTemplateId，必须由用户明确选择。</summary>
        public static bool HasAmbiguousDeploymentState()
        {
            var data = ReadDeploymentData();
            return string.IsNullOrEmpty(data.activeTemplateId) && data.records.Count > 1;
        }

        /// <summary>只用于迁移旧多记录格式：明确认定哪个已有记录对应当前项目内容。</summary>
        public static void SetActiveDeployedTemplate(string templateId)
        {
            var data = ReadDeploymentData();
            if (!string.IsNullOrEmpty(data.activeTemplateId)
                && !string.Equals(data.activeTemplateId, templateId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"当前活动模板已是 [{data.activeTemplateId}]；v0.11.0 不支持直接切换模板。");
            }
            if (data.records.Find(record =>
                    string.Equals(record.templateId, templateId, StringComparison.Ordinal)) == null)
                throw new InvalidOperationException($"模板 [{templateId}] 没有可认定的部署记录。");

            data.activeTemplateId = templateId;
            WriteDeploymentData(data);
        }

        /// <summary>读取 dev 仓库「当前正在编辑的模板」记录（无记录返回 null）。</summary>
        public static EditingTemplateRecord GetEditingTemplate()
        {
            var path = ToFullPath(EditingRecordPath);
            if (!File.Exists(path)) return null;

            try
            {
                return JsonUtility.FromJson<EditingTemplateRecord>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"读取编辑记录失败 {EditingRecordPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>保留旧 API 调用兼容性；校验功能统一在项目中心内使用，不注册独立菜单。</summary>
        public static void ValidateGeneratedFiles()
        {
            EmberSetupWindow.ShowProjectValidation(true);
        }

        internal static IReadOnlyList<string> BusinessDirectories => TemplateDirNames;

        internal static string GetTemplateAssetsPath(string templateId)
        {
            ValidateNewTemplateId(templateId);
            var packagePath = GetResolvedPath(PACKAGE)
                ?? throw new InvalidOperationException("com.ember 未安装。");
            return Path.Combine(packagePath, "Templates~", templateId, "Assets");
        }

        /// <summary>按保存模板的剥离规则读取项目文件；预览期间不写磁盘。</summary>
        internal static byte[] ReadProjectTemplateBytes(string filePath, string relativePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            string[] names = relativePath == "Game/Scenes/FrameworkScene.unity"
                ? new[] { "RainbowHierarchyRuleset" }
                : relativePath == "Game/Scenes/MainScene.unity"
                    ? new[] { "UnitaskDeme", "OdinDemo", "FeelDemo" }
                    : null;
            if (names == null) return bytes;
            var stripped = StripSceneObjectLines(File.ReadAllLines(filePath).ToList(), names);
            return stripped == null ? bytes : new System.Text.UTF8Encoding(false).GetBytes(
                string.Join(Environment.NewLine, stripped) + Environment.NewLine);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>读取模板元数据；模板不存在或解析失败返回 null（解析失败会打警告）。</summary>
        private static TemplateInfo ReadTemplateJson(string packagePath, string templateId)
        {
            var jsonPath = Path.Combine(packagePath, "Templates~", templateId, "template.json");
            if (!File.Exists(jsonPath)) return null;

            try
            {
                // 必须显式 UTF-8 读取：中文 Windows 默认编码为 GBK，否则中文元数据会乱码
                return JsonUtility.FromJson<TemplateInfo>(File.ReadAllText(jsonPath, System.Text.Encoding.UTF8));
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"解析模板元数据失败 {jsonPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>写模板元数据（创建目录 + UTF-8 无 BOM）。</summary>
        private static void WriteTemplateJson(string packagePath, TemplateInfo info)
        {
            var tplRoot = Path.Combine(packagePath, "Templates~", info.id);
            Directory.CreateDirectory(tplRoot);
            EmberTemplateTransaction.WriteTemplateJson(
                Path.Combine(tplRoot, "template.json"),
                info);
        }

        private static void EnsurePlanIsCurrent(
            TemplateSyncPlan requested,
            TemplateSyncPlan current)
        {
            if (!EmberTemplateSceneMergePlanner.TryValidatePreviewPlan(
                    requested,
                    current,
                    out var error))
            {
                throw new InvalidOperationException(
                    "同步计划已过期；请重新预览。" + (string.IsNullOrEmpty(error) ? string.Empty : " " + error));
            }
        }

        private static void ValidateNewTemplateId(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId)
                || !Regex.IsMatch(templateId, @"^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant)
                || templateId == "."
                || templateId == "..")
            {
                throw new ArgumentException(
                    "模板 id 只能包含小写字母、数字、点、下划线和连字符，且必须以字母或数字开头。",
                    nameof(templateId));
            }
        }

        private static void EnsureParentCanBeBranched(
            string packagePath,
            TemplateInfo parent,
            List<TemplateInfo> templates)
        {
            if (parent.schemaVersion != EmberTemplateInheritanceEngine.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"父模板 [{parent.id}] 尚未初始化继承元数据，不能创建派生模板。");
            }

            var issue = EmberTemplateInheritanceEngine.ValidateTemplateGraph(templates)
                .Find(item => item.TemplateId == parent.id);
            if (issue != null)
                throw new InvalidOperationException(issue.Message);

            if (!string.IsNullOrEmpty(parent.parentId))
            {
                var parentRoot = Path.Combine(packagePath, "Templates~", parent.id);
                var parentPlan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                    parent,
                    templates,
                    Path.Combine(packagePath, "Templates~", parent.parentId, "Assets"),
                    Path.Combine(parentRoot, "ParentSnapshot~", "Assets"),
                    Path.Combine(parentRoot, "Assets"));
                if (parentPlan.Status != TemplateSyncStatus.Synced)
                {
                    throw new InvalidOperationException(
                        $"父模板 [{parent.id}] 当前状态为 {parentPlan.Status}，请先完成自身父同步。");
                }
            }

            var parentAssets = Path.Combine(packagePath, "Templates~", parent.id, "Assets");
            if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                    parentAssets,
                    out var liveHash,
                    out var validationError))
            {
                throw new InvalidOperationException(
                    $"父模板 [{parent.id}] 未通过资源/.meta/GUID 校验：{validationError}");
            }
            if (!string.Equals(liveHash, parent.contentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"父模板 [{parent.id}] 的磁盘内容与 metadata 不一致，不能创建派生模板。");
            }
            if (!string.Equals(parent.contentHash, parent.versionedContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"父模板 [{parent.id}] 有未版本化内容，请先 bump 版本。");
            }
        }

        private static void EnsureEditingCopyCanSave(TemplateInfo template)
        {
            var editing = GetEditingTemplate();
            if (editing == null)
            {
                throw new InvalidOperationException(
                    $"当前没有可验证的模板编辑副本；请先加载模板 [{template.id}] 再保存。");
            }
            if (!string.Equals(editing.templateId, template.id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"当前项目编辑的是模板 [{editing.templateId}]，不能覆盖模板 [{template.id}]。如需复制内容，请使用另存为新模板。");
            }
            if (EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, template))
            {
                throw new InvalidOperationException(
                    $"模板 [{template.id}] 的项目编辑副本已过期；请重新加载后再保存。");
            }
        }

        private static EditingTemplateRecord CreateEditingRecord(TemplateInfo template)
        {
            return new EditingTemplateRecord
            {
                templateId = template.id,
                templateVersion = template.version,
                contentHash = template.contentHash,
                loadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private static void CommitVersionMetadata(
            string packagePath,
            TemplateInfo original,
            TemplateInfo updated)
        {
            var templatesRoot = Path.Combine(packagePath, "Templates~");
            var templateRoot = Path.Combine(templatesRoot, updated.id);
            var stageRoot = Path.Combine(
                templatesRoot,
                $".{updated.id}.version-staging-{Guid.NewGuid():N}~");
            try
            {
                var stagedMetadata = Path.Combine(stageRoot, "template.json");
                EmberTemplateTransaction.WriteTemplateJson(stagedMetadata, updated);
                var targets = new List<TemplateTransactionTarget>
                {
                    new TemplateTransactionTarget(
                        stagedMetadata,
                        Path.Combine(templateRoot, "template.json"))
                };

                var editing = GetEditingTemplate();
                if (editing != null
                    && string.Equals(editing.templateId, original.id, StringComparison.Ordinal)
                    && !EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, original))
                {
                    var updatedEditing = new EditingTemplateRecord
                    {
                        templateId = editing.templateId,
                        templateVersion = updated.version,
                        contentHash = updated.contentHash,
                        loadedAt = editing.loadedAt
                    };
                    var stagedEditing = Path.Combine(stageRoot, "EmberEditingTemplate.json");
                    EmberTemplateTransaction.WriteJson(stagedEditing, updatedEditing);
                    targets.Add(new TemplateTransactionTarget(
                        stagedEditing,
                        ToFullPath(EditingRecordPath)));
                }

                EmberTemplateTransaction.CommitPreparedTargets(targets);
            }
            finally
            {
                TryCleanTransactionPath(stageRoot);
            }
        }

        /// <summary>解析 "x.y.z" 模板版本；格式异常按 0 处理。</summary>
        private static int[] ParseTemplateVersion(string version)
        {
            var parts = (version ?? "").Split('.');
            var v = new[] { 0, 0, 0 };
            for (int i = 0; i < v.Length && i < parts.Length; i++)
                int.TryParse(parts[i].Trim(), out v[i]);
            return v;
        }

        /// <summary>写操作前确认 metadata 已初始化，且模板存储没有绕过模板编辑器发生变化。</summary>
        private static void EnsureStoredContentMatchesMetadata(
            TemplateInfo info,
            string assetsPath,
            string operation)
        {
            if (info.schemaVersion != EmberTemplateInheritanceEngine.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"模板 [{info.id}] 尚未初始化继承元数据，不能执行{operation}。请先运行一次性 metadata 初始化。");
            }

            if (string.IsNullOrEmpty(info.contentHash))
                throw new InvalidOperationException($"模板 [{info.id}] 缺少 contentHash，不能执行{operation}。");

            var liveHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(assetsPath);
            if (!string.Equals(liveHash, info.contentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"模板 [{info.id}] 的磁盘内容与 metadata 不一致，不能执行{operation}。请先检查未记录的模板内容变更。");
            }
        }

        /// <summary>
        /// 把文件头重写为真实部署版本：格式 "Generated by Ember Setup vX.Y.Z (framework vX.Y.Z)"，
        /// 兼容旧格式（仅有模板版本号）。头标记同时是全文件框架所有权的标记（见 docs/dev/template-upgrade-system.md §二），只替换版本号子串、标记保留。
        /// 返回是否实际发生了替换（用于统计「标记刷新」数量）。
        /// </summary>
        private static bool RewriteVersionMarker(string fullPath, string version, string frameworkVersion)
        {
            if (string.IsNullOrEmpty(version)) return false;

            byte[] bytes;
            try { bytes = File.ReadAllBytes(fullPath); }
            catch { return false; }

            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var enc = new System.Text.UTF8Encoding(hasBom);
            string text = enc.GetString(bytes);
            if (!text.Contains("Generated by Ember Setup")) return false;

            string replacement = "Generated by Ember Setup v" + version;
            if (!string.IsNullOrEmpty(frameworkVersion))
                replacement += " (framework v" + frameworkVersion + ")";

            var replaced = Regex.Replace(text,
                @"Generated by Ember Setup v\d+\.\d+\.\d+( \(framework v\d+\.\d+\.\d+\))?",
                replacement);
            if (replaced == text) return false;

            try { File.WriteAllBytes(fullPath, enc.GetBytes(replaced)); }
            catch { return false; /* 写失败不影响部署，标记保持原样 */ }
            return true;
        }

        /// <summary>写入/更新消费端部署记录（upsert：templateId + version + frameworkVersion + deployedAt）。</summary>
        private static void RecordDeployment(string packagePath, string templateId)
        {
            var info = ReadTemplateJson(packagePath, templateId);
            if (info == null) return;

            var data = ReadDeploymentData();

            var record = data.records.Find(r => r.templateId == templateId);
            if (record == null)
            {
                record = new DeployedTemplateRecord { templateId = templateId };
                data.records.Add(record);
            }
            record.version = info.version;
            record.frameworkVersion = info.frameworkVersion;
            record.deployedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            data.activeTemplateId = templateId;

            WriteDeploymentData(data);
        }

        internal static DeployedTemplateRecord ResolveActiveDeployment(
            DeployedTemplatesData data)
        {
            if (data == null || data.records == null || data.records.Count == 0)
                return null;
            if (!string.IsNullOrEmpty(data.activeTemplateId))
            {
                return data.records.Find(record => string.Equals(
                    record.templateId,
                    data.activeTemplateId,
                    StringComparison.Ordinal));
            }

            // v0.10 旧格式只有一条记录时可无歧义地在内存中兼容；不在读取时写盘。
            return data.records.Count == 1 ? data.records[0] : null;
        }

        internal static string GetDeploymentBlockReason(
            DeployedTemplatesData data,
            string requestedTemplateId)
        {
            data ??= new DeployedTemplatesData();
            data.records ??= new List<DeployedTemplateRecord>();
            if (!string.IsNullOrEmpty(data.activeTemplateId))
            {
                var explicitActive = data.records.Find(record => string.Equals(
                    record.templateId,
                    data.activeTemplateId,
                    StringComparison.Ordinal));
                if (explicitActive == null)
                {
                    return $"部署记录的 activeTemplateId [{data.activeTemplateId}] 没有对应记录，请先人工修复。";
                }
                return string.Equals(
                    explicitActive.templateId,
                    requestedTemplateId,
                    StringComparison.Ordinal)
                    ? null
                    : $"当前活动模板是 [{explicitActive.templateId}]；切换到 [{requestedTemplateId}] 需要迁移，v0.11.0 禁止直接覆盖部署。";
            }

            if (data.records.Count == 0) return null;
            if (data.records.Count == 1)
            {
                return string.Equals(
                    data.records[0].templateId,
                    requestedTemplateId,
                    StringComparison.Ordinal)
                    ? null
                    : $"旧部署记录表明当前项目来自 [{data.records[0].templateId}]；切换到 [{requestedTemplateId}] 需要迁移。";
            }

            return "旧部署记录包含多个模板且没有 activeTemplateId；请先在项目中心明确认定当前活动模板。";
        }

        private static DeployedTemplatesData ReadDeploymentData()
        {
            var path = ToFullPath(DeployedRecordsPath);
            if (!File.Exists(path)) return new DeployedTemplatesData();
            try
            {
                var data = JsonUtility.FromJson<DeployedTemplatesData>(File.ReadAllText(path))
                    ?? new DeployedTemplatesData();
                data.records ??= new List<DeployedTemplateRecord>();
                return data;
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"读取部署记录失败 {DeployedRecordsPath}: {ex.Message}");
                return new DeployedTemplatesData();
            }
        }

        private static void WriteDeploymentData(DeployedTemplatesData data)
        {
            var path = ToFullPath(DeployedRecordsPath);
            var stagePath = path + $".staging-{Guid.NewGuid():N}~";
            try
            {
                EmberTemplateTransaction.WriteJson(stagePath, data);
                EmberTemplateTransaction.CommitPreparedTargets(
                    new[] { new TemplateTransactionTarget(stagePath, path) });
            }
            finally
            {
                TryCleanTransactionPath(stagePath);
            }
        }

        private static DeployedTemplateRecord CloneDeploymentRecord(DeployedTemplateRecord source)
        {
            if (source == null) return null;
            return new DeployedTemplateRecord
            {
                templateId = source.templateId,
                version = source.version,
                frameworkVersion = source.frameworkVersion,
                deployedAt = source.deployedAt
            };
        }

        private static void ClearEditingRecord()
        {
            var path = ToFullPath(EditingRecordPath);
            if (!File.Exists(path) && !File.Exists(path + ".meta")) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
            }
            catch { /* 清理失败不阻断 */ }
        }

        private static string GetResolvedPath(string packageName)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            return info?.resolvedPath;
        }

        /// <summary>删除目录（不存在或删除失败静默——用于暂存区/备份清理，不阻断主流程）。</summary>
        private static void CleanDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            try { Directory.Delete(path, true); } catch { /* 清理失败不阻断 */ }
        }

        private static void TryCleanTransactionPath(string path)
        {
            try { EmberTemplateTransaction.CleanPath(path); }
            catch { /* 暂存清理失败不覆盖主事务结果。 */ }
        }

        /// <summary>整树复制模板到项目 Assets/。返回部署的文件数。</summary>
        private static int DeployTemplate(string packagePath, string templateId)
        {
            var srcRoot = Path.Combine(packagePath, "Templates~", templateId, "Assets");
            if (!Directory.Exists(srcRoot))
            {
                EmberDebug.LogWarning(TAG, $"模板缺失：{srcRoot}");
                return 0;
            }

            var tplInfo = ReadTemplateJson(packagePath, templateId);

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null) return 0;

            int deployed = 0;
            int refreshed = 0;

            // 拷贝期间挂起自动导入：Unity 运行中 File.Copy 落盘瞬间，文件监视器会先为新文件生成随机 GUID 的 .meta，
            // 真实 .meta 随后到达时已造成 GUID 错位（场景预制体实例/脚本引用全断）。全部文件（含 .meta）落盘后一次性导入。
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                foreach (var file in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
                {
                    var rel = file.Substring(srcRoot.Length + 1);
                    var dest = Path.Combine(projectRoot, "Assets", rel);
                    if (File.Exists(dest))
                    {
                        // 已有文件不覆盖内容：仅刷新头标记版本（标记刷新；无头标记的用户文件不受影响）
                        if (tplInfo != null
                            && RewriteVersionMarker(dest, tplInfo.version, tplInfo.frameworkVersion))
                        {
                            refreshed++;
                        }
                        continue;
                    }

                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.Copy(file, dest, false);
                    if (tplInfo != null)
                        RewriteVersionMarker(dest, tplInfo.version, tplInfo.frameworkVersion);
                    deployed++;
                }
            }
            finally
            {
                AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            EmberDebug.LogInit(TAG,
                $"模板 [{templateId}] 部署完成：新增 {deployed} 个文件，刷新头标记 {refreshed} 个。");
            return deployed;
        }

        private static void RegisterBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // FrameworkScene 固定 index 0
            if (!scenes.Any(s => s.path == FrameworkScenePath))
                scenes.Insert(0, new EditorBuildSettingsScene(FrameworkScenePath, true));

            // 其余场景按 Assets/Game/Scenes 扫描顺序补入（仅未注册的）
            if (Directory.Exists(ToFullPath(ScenesDir)))
            {
                foreach (var f in Directory.GetFiles(ToFullPath(ScenesDir), "*.unity"))
                {
                    var assetPath = ToAssetPath(f);
                    if (assetPath == FrameworkScenePath) continue;
                    if (!scenes.Any(s => s.path == assetPath))
                        scenes.Add(new EditorBuildSettingsScene(assetPath, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null) return assetPath;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null) return fullPath;
            var rel = fullPath.Substring(projectRoot.Length + 1).Replace('\\', '/');
            return rel;
        }

        /// <summary>把项目业务层复制到模板暂存 Assets，并剥离只属于 dev 仓库的场景对象。</summary>
        private static int CopyProjectBusinessLayer(string projectRoot, string stagedAssets)
        {
            Directory.CreateDirectory(stagedAssets);
            int count = 0;
            foreach (var rel in TemplateDirNames)
            {
                var normalizedRel = rel.Replace('/', Path.DirectorySeparatorChar);
                var source = Path.Combine(projectRoot, "Assets", normalizedRel);
                if (!Directory.Exists(source)) continue;
                count += EmberTemplateTransaction.CopyDirectory(
                    source,
                    Path.Combine(stagedAssets, normalizedRel));
            }

            StripSceneObjects(
                Path.Combine(stagedAssets, "Game", "Scenes", "FrameworkScene.unity"),
                "RainbowHierarchyRuleset");
            StripSceneObjects(
                Path.Combine(stagedAssets, "Game", "Scenes", "MainScene.unity"),
                "UnitaskDeme",
                "OdinDemo",
                "FeelDemo");
            return count;
        }

        /// <summary>从场景文件中移除指定名字的对象（连同组件块与父级 children 引用）。</summary>
        private static void StripSceneObjects(string scenePath, params string[] names)
        {
            if (!File.Exists(scenePath)) return;
            var stripped = StripSceneObjectLines(File.ReadAllLines(scenePath).ToList(), names);
            if (stripped != null)
                File.WriteAllLines(scenePath, stripped, new System.Text.UTF8Encoding(false));
        }

        private static List<string> StripSceneObjectLines(List<string> lines, string[] names)
        {
            var blockStart = new List<int>();
            for (int i = 0; i < lines.Count; i++)
                if (lines[i].StartsWith("--- !u!")) blockStart.Add(i);

            var goIds = new HashSet<string>();
            var compIds = new HashSet<string>();
            foreach (var name in names)
            {
                for (int b = 0; b < blockStart.Count; b++)
                {
                    int s = blockStart[b];
                    int e = (b + 1 < blockStart.Count) ? blockStart[b + 1] : lines.Count;
                    if (!lines[s].StartsWith("--- !u!1 &")) continue;

                    string goId = lines[s].Substring(lines[s].LastIndexOf('&') + 1).Trim();
                    bool matched = false;
                    for (int i = s; i < e; i++)
                    {
                        if (lines[i].Trim() == "m_Name: " + name) { matched = true; break; }
                    }
                    if (!matched) continue;

                    goIds.Add(goId);
                    for (int i = s; i < e; i++)
                    {
                        var m = Regex.Match(lines[i], "component: \\{fileID: (\\d+)\\}");
                        if (m.Success) compIds.Add(m.Groups[1].Value);
                    }
                }
            }

            if (goIds.Count == 0) return null;

            var delete = new HashSet<int>();
            for (int b = 0; b < blockStart.Count; b++)
            {
                int s = blockStart[b];
                int e = (b + 1 < blockStart.Count) ? blockStart[b + 1] : lines.Count;
                if (lines[s].StartsWith("--- !u!1 &"))
                {
                    string goId = lines[s].Substring(lines[s].LastIndexOf('&') + 1).Trim();
                    if (goIds.Contains(goId))
                        for (int i = s; i < e; i++) delete.Add(i);
                }
                else
                {
                    for (int i = s; i < e; i++)
                    {
                        var m = Regex.Match(lines[i], "m_GameObject: \\{fileID: (\\d+)\\}");
                        if (m.Success && goIds.Contains(m.Groups[1].Value))
                        {
                            for (int j = s; j < e; j++) delete.Add(j);
                            break;
                        }
                    }
                }
            }

            var outLines = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (delete.Contains(i)) continue;
                var line = lines[i];
                var cm = Regex.Match(line, "- \\{fileID: (\\d+)\\}");
                if (cm.Success && compIds.Contains(cm.Groups[1].Value)) continue;
                outLines.Add(line);
            }

            return outLines;
        }

        #endregion
    }

}
