using System;
using System.Reflection;
using Ember.Core;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>玩家专属编辑入口的统一模板和模块边界。</summary>
    [InitializeOnLoad]
    internal static class PlayerEditorAccess
    {
        #region 内部参数
        internal const string MENU_PATH = "Ember/Player/玩家开发工具";
        internal static bool TemplateAvailable => EmberProjectSetup.IsTemplateActive("platformer-2d");
        internal static bool Visible => TemplateAvailable && EmberExtensionProject.IsEnabled(EmberExtensionProject.FsmBlueprint);
        internal static bool Enabled => typeof(PlayerModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled == true;
        internal static bool Available => Visible && Enabled;
        internal static string CurrentVersion => EmberProjectSetup.IsEmbeddedPackage()
            ? EmberProjectSetup.GetEditingTemplate()?.templateVersion : EmberProjectSetup.GetActiveDeployedTemplate()?.version;
        internal static bool AllowsWindow(string owner, string version) => Available && owner == EmberFsmBlueprintAccess.CurrentTemplateId && version == CurrentVersion;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerEditorAccess()
        {
            EmberConditionalMenu.Register(MENU_PATH, () => Available, () => PlayerSchemeWindow.OpenTab(0));
            EmberExtensionDocuments.RegisterModuleScope("platformer.player", "platformer-2d", () => Enabled);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Require()
        {
            if (!TemplateAvailable || !Enabled) throw new InvalidOperationException("此工具需要活动平台模板及启用的 Player 模块。");
            EmberExtensionProject.Require(EmberExtensionProject.FsmBlueprint);
        }
        internal static void RequireSession(bool formalAcceptance)
        {
            if (!TemplateAvailable || !Enabled) throw new InvalidOperationException("试玩需要活动平台模板及启用的 Player 模块。");
            EmberExtensionProject.Require(formalAcceptance ? EmberExtensionProject.LevelTrial : EmberExtensionProject.FsmBlueprint);
        }
        internal static bool DrawUnavailable(string owner = null, string version = null)
        {
            if (Available && (owner == null || AllowsWindow(owner, version))) return false;
            EditorGUILayout.HelpBox("扩展、模板版本或 Player 模块不满足本窗口的隔离声明；草稿保留，编辑与试跑暂停。", MessageType.Info);
            return true;
        }
        #endregion
    }
}
