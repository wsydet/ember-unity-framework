// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Ember.UPMManager.Editor
{
    public partial class EmberUPMManager
    {
        #region 内部方法
        [InitializeOnLoadMethod]
        private static void ScheduleBundledAiSkills()
        {
            UnityEditor.PackageManager.Events.registeredPackages -= OnProjectRulesPackagesRegistered;
            UnityEditor.PackageManager.Events.registeredPackages += OnProjectRulesPackagesRegistered;
            EditorApplication.delayCall += SyncProjectRules;
            EditorApplication.delayCall += InstallBundledAiSkills;
        }

        private static void OnProjectRulesPackagesRegistered(UnityEditor.PackageManager.PackageRegistrationEventArgs args)
        {
            if (args == null || !(args.added.Any(p => p != null && p.name == "com.ember")
                || args.changedTo.Any(p => p != null && p.name == "com.ember"))) return;
            EditorApplication.delayCall -= SyncProjectRules;
            EditorApplication.delayCall += SyncProjectRules;
        }

        private static void SyncProjectRules()
        {
            EditorApplication.delayCall -= SyncProjectRules;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += SyncProjectRules;
                return;
            }
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.ember");
            if (package == null) return;
            try
            {
                string status = EmberProjectRulesSync.Sync(AiSkillProjectRoot, package.resolvedPath, out bool hasErrors);
                SessionState.SetString(EmberProjectRulesSync.STATUS_KEY, status);
                SessionState.SetBool(EmberProjectRulesSync.ERROR_KEY, hasErrors);
            }
            catch (Exception ex)
            {
                SessionState.SetString(EmberProjectRulesSync.STATUS_KEY, "项目公共规则入口同步失败：" + ex.Message);
                SessionState.SetBool(EmberProjectRulesSync.ERROR_KEY, true);
            }
        }

        private static void InstallBundledAiSkills()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += InstallBundledAiSkills;
                return;
            }
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.ember");
            if (package == null || EmberAISkillInstaller.IsSourceProject(AiSkillProjectRoot)) return;
            string bundle = Path.Combine(package.resolvedPath, "AISkills~");
            if (!File.Exists(Path.Combine(bundle, "bundle.json"))) return;
            string key = "Ember.AISkills.Bootstrap:" + AiSkillProjectRoot + ":" + package.version;
            if (SessionState.GetBool(key, false)) return;
            SessionState.SetBool(key, true);
            try
            {
                var messages = EmberAISkillInstaller.InstallBundledMissing(AiSkillProjectRoot, bundle, package.version, HasEuiRegenerateApi());
                SessionState.SetString("Ember.AISkills.BootstrapStatus", string.Join("\n", messages));
            }
            catch (Exception ex)
            {
                SessionState.SetString("Ember.AISkills.BootstrapStatus", "框架附带技能未全部安装：" + ex.Message
                    + "\n请在 UPM Manager 检查，已有项目内容保持保护。");
            }
        }
        #endregion
    }
}
