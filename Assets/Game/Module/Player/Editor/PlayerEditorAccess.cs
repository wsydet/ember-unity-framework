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
        internal static bool Visible => EmberProjectSetup.IsTemplateActive("platformer-2d");
        internal static bool Enabled => typeof(PlayerModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled == true;
        internal static bool Available => Visible && Enabled;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerEditorAccess() => EmberConditionalMenu.Register(MENU_PATH, () => Visible,
            () => PlayerSchemeWindow.OpenTab(0), () => Enabled);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Require()
        {
            if (!Available) throw new InvalidOperationException(Visible ? "Player 模块已关闭。" : "此工具仅用于活动的 2D 平台模板体系。");
        }
        internal static bool DrawUnavailable()
        {
            if (Available) return false;
            EditorGUILayout.HelpBox(Visible ? "Player 模块已关闭；草稿保留，编辑与试跑暂停。"
                : "当前不属于 2D 平台模板体系；草稿保留，编辑与试跑暂停。", MessageType.Info);
            return true;
        }
        #endregion
    }
}
