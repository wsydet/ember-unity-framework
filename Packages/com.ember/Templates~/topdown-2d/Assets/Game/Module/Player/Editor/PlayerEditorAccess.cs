using System;
using Ember.Core;
using Ember.Core.Editor;
using UnityEditor;

namespace Game.Player.Editor
{
    internal static class PlayerEditorAccess
    {
        #region 外部方法
        internal static bool ModuleEnabled=>((EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(PlayerModule),typeof(EmberModuleAttribute)))?.Enabled==true;
        internal static bool Available=>EmberExtensionProject.IsEnabled(EmberExtensionProject.FsmBlueprint)&&EmberProjectSetup.IsTemplateActive("topdown-2d")&&ModuleEnabled;
        internal static void RequireWrite()
        {
            if(!Available||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)
                throw new InvalidOperationException("俯视玩家工具需要所属活动模板、启用 Player 模块及空闲编辑状态。");
        }
        #endregion
    }
}
