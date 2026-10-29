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
            string path = EmberExtensionProject.ConfigurationPath;
            string signature = path + ":" + File.GetLastWriteTimeUtc(path).Ticks + ":"
                + File.GetLastWriteTimeUtc(EmberExtensionProject.DefaultsPath).Ticks + ":" + EmberExtensionProject.CurrentTemplateVersion
                + ":" + string.Join(",", EmberExtensionDocuments.EnabledScopes)
                + ":" + File.GetLastWriteTimeUtc("Assets/" + EmberAISkillInstaller.TemplateSourceDirectory + "/catalog.json").Ticks;
            if (_signature == signature) return;
            _signature = signature;
            try
            {
                var preview = EmberProjectSetup.PreviewExtensionSkills(EmberExtensionProject.Load());
                if (preview == null) return;
                EmberAISkillInstaller.LimitToUnavailableExtensions(preview);
                if (preview.HasChanges) EmberProjectSetup.CommitExtensionDiscoveryPruning(preview);
                Error = null;
            }
            catch (Exception exception)
            {
                Error = "扩展发现副本未能同步；执行前检查仍会阻止失效入口。" + exception.Message;
                EmberDebug.LogWarning("EmberExtension", Error);
            }
        }
        #endregion
    }
}
