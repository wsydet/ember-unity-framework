using System;
using System.IO;
using Ember.Basic;
using Ember.UPMManager.Editor;
using UnityEditor;

namespace Ember.Core.Editor
{
    /// <summary>配置/正式身份变化及域重载后收回失效发现副本；源与草稿保留。</summary>
    [InitializeOnLoad]
    internal static class EmberExtensionDiscovery
    {
        #region 内部参数
        private static string _signature;
        private static double _nextCheck;
        internal static string Error { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberExtensionDiscovery() { EditorApplication.update += Tick; }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _nextCheck = EditorApplication.timeSinceStartup + 2;
            try
            {
                string path = EmberExtensionProject.ConfigurationPath;
                string root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
                string signature = path + ":" + File.GetLastWriteTimeUtc(path).Ticks + ":"
                    + File.GetLastWriteTimeUtc(EmberExtensionProject.DefaultsPath).Ticks + ":" + EmberExtensionProject.CurrentTemplateVersion
                    + ":" + string.Join(",", EmberExtensionDocuments.EnabledScopes)
                    + ":" + File.GetLastWriteTimeUtc("Assets/Editor/EmberEditingTemplate.json").Ticks
                    + ":" + File.GetLastWriteTimeUtc("Assets/Editor/EmberDeployedTemplates.json").Ticks
                    + ":" + File.GetLastWriteTimeUtc("Assets/" + EmberAISkillInstaller.TemplateSourceDirectory + "/catalog.json").Ticks
                    + ":" + EmberAISkillInstaller.ProjectSkillSourceFingerprint(root);
                if (_signature == signature) return;
                _signature = signature;
                var preview = EmberProjectSetup.PreviewExtensionSkills(EmberExtensionProject.Load());
                if (preview == null) return;
                EmberAISkillInstaller.LimitToUnavailableExtensions(preview);
                if (preview.HasChanges) EmberProjectSetup.CommitExtensionDiscoveryPruning(preview);
                Error = null;
            }
            catch (Exception exception)
            {
                try
                {
                    var unavailable = EmberAISkillInstaller.PreviewUnavailableProjectSkills(Directory.GetParent(UnityEngine.Application.dataPath).FullName);
                    if (unavailable.HasChanges) EmberProjectSetup.CommitExtensionDiscoveryPruning(unavailable);
                }
                catch (Exception cleanup) { exception = new AggregateException(exception, cleanup); }
                Error = "扩展发现副本未能同步；执行前检查仍会阻止失效入口。" + exception.Message;
                EmberDebug.LogWarning("EmberExtension", Error);
            }
        }
        #endregion
    }
}
