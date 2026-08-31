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

        /// <summary>模板快照覆盖的业务层目录（相对项目 Assets/）。</summary>
        private static readonly string[] TemplateDirNames = { "Game", "Resources", "Ember/Editor", "Settings" };

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>执行完整初始化：部署指定模板 + 注册场景 + 刷新场景映射。返回部署的文件数。</summary>
        public static int Initialize(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return 0;

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
            // 当前只有基础模板：以 4 场景 + 演示状态类 + GM 页为标记
            return File.Exists(ToFullPath(FrameworkScenePath))
                && File.Exists(ToFullPath(MainScenePath))
                && File.Exists(ToFullPath(GameplayScenePath))
                && File.Exists(ToFullPath(SettingsScenePath))
                && File.Exists(ToFullPath("Assets/Game/State/GameMainState.cs"));
        }

        /// <summary>新建模板。fromBase=true 时复制基础模板内容作为起点；返回复制的文件数（0=空模板）。</summary>
        public static int CreateTemplate(string templateId, string displayName, string description, bool fromBase)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            if (packagePath == null) return -1;

            var tplRoot = Path.Combine(packagePath, "Templates~", templateId);
            if (Directory.Exists(tplRoot))
                throw new InvalidOperationException($"模板 [{templateId}] 已存在，请换一个 id。");

            Directory.CreateDirectory(Path.Combine(tplRoot, "Assets"));

            int n = 0;
            if (fromBase)
            {
                var baseAssets = Path.Combine(packagePath, "Templates~", "base", "Assets");
                if (!Directory.Exists(baseAssets))
                    throw new InvalidOperationException("基础模板（base）不存在，无法复制起点。");

                var dst = Path.Combine(tplRoot, "Assets");
                foreach (var file in Directory.GetFiles(baseAssets, "*", SearchOption.AllDirectories))
                {
                    var rel = file.Substring(baseAssets.Length + 1);
                    var dest = Path.Combine(dst, rel);
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.Copy(file, dest, true);
                    n++;
                }
            }

            var info = new TemplateInfo
            {
                id = templateId,
                displayName = string.IsNullOrEmpty(displayName) ? templateId : displayName,
                description = description ?? "",
                version = InitialTemplateVersion,
                frameworkVersion = GetFrameworkVersion(),
                channel = ChannelStable,
                order = 99
            };
            WriteTemplateJson(packagePath, info);

            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 已创建（{(fromBase ? $"复制基础模板 {n} 文件" : "空模板")}）。");
            return n;
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
            if (Directory.Exists(tplAssets)) Directory.Delete(tplAssets, true);
            Directory.CreateDirectory(tplAssets);

            int n = 0;
            foreach (var rel in TemplateDirNames)
            {
                var src = Path.Combine(projectRoot, "Assets", rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(src)) continue;

                var dst = Path.Combine(tplAssets, rel.Replace('/', Path.DirectorySeparatorChar));
                foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
                {
                    var relFile = file.Substring(src.Length + 1);
                    var dest = Path.Combine(dst, relFile);
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.Copy(file, dest, true);
                    n++;
                }
            }

            // 剥离 dev 测试对象（第三方/开发工具，不进模板）
            StripSceneObjects(Path.Combine(tplAssets, "Game", "Scenes", "FrameworkScene.unity"), "RainbowHierarchyRuleset");
            StripSceneObjects(Path.Combine(tplAssets, "Game", "Scenes", "MainScene.unity"), "UnitaskDeme", "OdinDemo", "FeelDemo");

            // 写 template.json：模板版本独立于框架版本——已存在则保留原版本/排序，新模板从 InitialTemplateVersion 起
            var existing = ReadTemplateJson(packagePath, templateId);
            var info = new TemplateInfo
            {
                id = templateId,
                displayName = string.IsNullOrEmpty(displayName) ? templateId : displayName,
                description = description ?? "",
                version = existing != null ? existing.version : InitialTemplateVersion,
                frameworkVersion = GetFrameworkVersion(),
                channel = existing != null && !string.IsNullOrEmpty(existing.channel) ? existing.channel : ChannelStable,
                order = existing != null ? existing.order : 1
            };
            WriteTemplateJson(packagePath, info);

            EmberDebug.LogInit(TAG, $"模板 [{templateId}] 已保存（{n} 文件）。");
            return n;
        }

        /// <summary>把模板内容加载到项目业务层（替换当前 Game/Resources/Ember/Editor/Settings，供编辑）。返回复制文件数。</summary>
        public static int LoadTemplate(string templateId)
        {
            var packagePath = GetResolvedPath(PACKAGE);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (packagePath == null || projectRoot == null) return -1;

            var tplAssets = Path.Combine(packagePath, "Templates~", templateId, "Assets");
            if (!Directory.Exists(tplAssets)) return -1;

            int n = 0;
            foreach (var rel in TemplateDirNames)
            {
                var dst = Path.Combine(projectRoot, "Assets", rel.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(dst)) Directory.Delete(dst, true);

                var src = Path.Combine(tplAssets, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(src)) continue;

                foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
                {
                    var relFile = file.Substring(src.Length + 1);
                    var dest = Path.Combine(dst, relFile);
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.Copy(file, dest, true);
                    n++;
                }
            }

            AssetDatabase.Refresh();
            RecordEditingTemplate(templateId);
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

            var v = ParseTemplateVersion(info.version);
            switch (field)
            {
                case 0: v[0]++; v[1] = 0; v[2] = 0; break;
                case 1: v[1]++; v[2] = 0; break;
                case 2: v[2]++; break;
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }

            info.version = $"{v[0]}.{v[1]}.{v[2]}";
            WriteTemplateJson(packagePath, info);
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

            info.version = version;
            WriteTemplateJson(packagePath, info);
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

            info.frameworkVersion = GetFrameworkVersion();
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
            var path = ToFullPath(DeployedRecordsPath);
            if (!File.Exists(path)) return null;

            try
            {
                var data = JsonUtility.FromJson<DeployedTemplatesData>(File.ReadAllText(path));
                return data?.records?.Find(r => r.templateId == templateId);
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"读取部署记录失败 {DeployedRecordsPath}: {ex.Message}");
                return null;
            }
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

        [MenuItem("Ember/Setup/校验生成物一致性", false, 101)]
        public static void ValidateGeneratedFiles()
        {
            var report = new List<string>();

            void CheckFile(string path)
            {
                report.Add($"{path}\n    {(File.Exists(ToFullPath(path)) ? "✅ 存在" : "⚠️ 缺失（重跑初始化补全）")}");
            }

            CheckFile(FrameworkScenePath);
            CheckFile(MainScenePath);
            CheckFile(GameplayScenePath);
            CheckFile(SettingsScenePath);
            CheckFile(SceneMappingPath);
            CheckFile("Assets/Game/State/GameMainState.cs");
            CheckFile("Assets/Game/UI/GamePages.cs");
            CheckFile("Assets/Game/UI/GamePages.User.cs");
            CheckFile("Assets/Game/UI/Runtime/Prefabs/GMPage.prefab");

            EditorUtility.DisplayDialog("生成物一致性校验", string.Join("\n", report), "确定");
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
            File.WriteAllText(Path.Combine(tplRoot, "template.json"),
                JsonUtility.ToJson(info, true), new System.Text.UTF8Encoding(false));
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

            var path = ToFullPath(DeployedRecordsPath);
            var data = new DeployedTemplatesData();
            try
            {
                if (File.Exists(path))
                    data = JsonUtility.FromJson<DeployedTemplatesData>(File.ReadAllText(path)) ?? data;
            }
            catch { /* 记录损坏则重建 */ }

            var record = data.records.Find(r => r.templateId == templateId);
            if (record == null)
            {
                record = new DeployedTemplateRecord { templateId = templateId };
                data.records.Add(record);
            }
            record.version = info.version;
            record.frameworkVersion = info.frameworkVersion;
            record.deployedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(data, true), new System.Text.UTF8Encoding(false));
        }

        /// <summary>记录「当前正在编辑的模板」（LoadTemplate 后调用）。</summary>
        private static void RecordEditingTemplate(string templateId)
        {
            var path = ToFullPath(EditingRecordPath);
            var record = new EditingTemplateRecord
            {
                templateId = templateId,
                loadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(record, true), new System.Text.UTF8Encoding(false));
        }

        private static void ClearEditingRecord()
        {
            var path = ToFullPath(EditingRecordPath);
            if (!File.Exists(path)) return;
            try { File.Delete(path); }
            catch { /* 清理失败不阻断 */ }
        }

        private static string GetResolvedPath(string packageName)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            return info?.resolvedPath;
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
                        AssetDatabase.ImportAsset("Assets/" + rel.Replace('\\', '/'));
                    }
                    continue;
                }

                var dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(file, dest, false);
                if (tplInfo != null)
                    RewriteVersionMarker(dest, tplInfo.version, tplInfo.frameworkVersion);
                var assetPath = "Assets/" + rel.Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath);
                deployed++;
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

        /// <summary>从场景文件中移除指定名字的对象（连同组件块与父级 children 引用）。</summary>
        private static void StripSceneObjects(string scenePath, params string[] names)
        {
            if (!File.Exists(scenePath)) return;
            var lines = File.ReadAllLines(scenePath).ToList();

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

            if (goIds.Count == 0) return;

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

            File.WriteAllLines(scenePath, outLines, new System.Text.UTF8Encoding(false));
        }

        #endregion
    }

    /// <summary>模板元数据（Templates~/&lt;模板名&gt;/template.json）。</summary>
    [Serializable]
    public class TemplateInfo
    {
        public string id;
        public string displayName;
        public string description;
        public string version;
        public string frameworkVersion;
        public string channel;
        public int order;
    }

    /// <summary>消费端已部署模板记录（Assets/Editor/EmberDeployedTemplates.json）。</summary>
    [Serializable]
    public class DeployedTemplateRecord
    {
        public string templateId;
        public string version;
        public string frameworkVersion;
        public string deployedAt;
    }

    /// <summary>部署记录文件反序列化结构。</summary>
    [Serializable]
    internal class DeployedTemplatesData
    {
        public List<DeployedTemplateRecord> records = new();
    }

    /// <summary>dev 仓库「当前正在编辑的模板」记录（Assets/Editor/EmberEditingTemplate.json）。</summary>
    [Serializable]
    public class EditingTemplateRecord
    {
        public string templateId;
        public string loadedAt;
    }

    /// <summary>模板升级等级（已部署版本 → 包内版本）：major=弃用重写 / minor=结构变化 / patch=修复。</summary>
    public enum TemplateUpgradeLevel
    {
        None,
        Patch,
        Minor,
        Major
    }
}
