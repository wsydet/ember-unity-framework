// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Ember.Core.Editor
{
    /// <summary>蓝图工具的模板能力登记。模板身份只来自项目中心，不扫描残留资产猜测。</summary>
    [InitializeOnLoad]
    public static class EmberFsmBlueprintAccess
    {
        #region 内部参数
        public const string MenuPath = "Ember/FSM/蓝图观察器";
        private sealed class Scope : IDisposable
        {
            internal string Owner;
            internal Func<bool> Enabled;
            public void Dispose() => Scopes.Remove(this);
        }
        private static readonly List<Scope> Scopes = new();
        #endregion
        #region 内部方法
        static EmberFsmBlueprintAccess()
        { EmberConditionalMenu.Register(MenuPath, () => IsAvailable, EmberFsmBlueprintWindow.OpenSelected); }
        #endregion
        #region 外部方法
        public static string CurrentTemplateId
        {
            get
            {
                var id = EmberProjectSetup.IsEmbeddedPackage() ? EmberProjectSetup.GetEditingTemplate()?.templateId
                    : EmberProjectSetup.GetActiveDeployedTemplate()?.templateId;
                return !string.IsNullOrEmpty(id) && EmberProjectSetup.IsTemplateActive(id) ? id : null;
            }
        }
        public static bool IsAvailable => EmberExtensionProject.IsEnabled(EmberExtensionProject.FsmBlueprint);
        public static bool Allows(string ownerTemplateId, Func<bool> enabled = null)
            => EmberExtensionProject.Allows(EmberExtensionProject.FsmBlueprint, ownerTemplateId, enabled ?? (() => true));
        /// <summary>没有参数/观察扩展的模板也可显式登记蓝图能力；模块禁用时条件必须返回 false。</summary>
        public static IDisposable Register(string ownerTemplateId, Func<bool> enabled)
        {
            if (string.IsNullOrWhiteSpace(ownerTemplateId)) throw new ArgumentException("必须声明所属模板。", nameof(ownerTemplateId));
            var scope = new Scope {Owner=ownerTemplateId,Enabled=enabled ?? throw new ArgumentNullException(nameof(enabled))};
            Scopes.Add(scope); return scope;
        }
        #endregion
    }
}
