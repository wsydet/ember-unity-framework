using System;
using Ember.Core.Editor;
using UnityEditor;

namespace Ember.LevelAuthoring.Editor
{
    [InitializeOnLoad]
    public static class EmberLevelAccess
    {
        #region 内部方法
        static EmberLevelAccess()
        {
            EmberConditionalMenu.Register("Ember/Level/2D 关卡工作台", () => Available, EmberLevelWorkbench.Open);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static bool Available => EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelAuthoring);
        public static void RequireWrite() => EmberExtensionProject.Require(EmberExtensionProject.LevelAuthoring, true);
        public static bool Allows(string owner, Func<bool> enabled)
        {
            if (!Available || owner == null || owner != "" && !EmberProjectSetup.IsTemplateActive(owner)) return false;
            try { return enabled?.Invoke() ?? false; } catch (Exception) { return false; }
        }
        #endregion
    }
}
