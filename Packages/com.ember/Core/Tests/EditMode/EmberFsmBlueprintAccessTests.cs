// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Reflection;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Ember.Core.Fsm.Tests
{
    public sealed class EmberFsmBlueprintAccessTests
    {
        private string _configurationPath;
        private byte[] _configurationBytes;

        [SetUp] public void SetUp()
        {
            _configurationPath = EmberExtensionProject.ConfigurationPath;
            _configurationBytes = File.Exists(_configurationPath) ? File.ReadAllBytes(_configurationPath) : null;
            // The host template may intentionally have every optional extension disabled.
            var config = EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.FsmBlueprint });
            File.WriteAllText(_configurationPath, JsonUtility.ToJson(config));
            EmberConditionalMenu.Refresh();
        }

        [TearDown] public void TearDown()
        {
            if (_configurationBytes == null) File.Delete(_configurationPath);
            else File.WriteAllBytes(_configurationPath, _configurationBytes);
            EmberConditionalMenu.Refresh();
        }

        [Test] public void UnknownOwnerAndDisabledModuleCannotGrantAccess()
        {
            Assert.IsFalse(EmberFsmBlueprintAccess.Allows(null));
            Assert.IsFalse(EmberFsmBlueprintAccess.Allows("missing-template-test"));
            Assert.IsFalse(EmberFsmBlueprintAccess.Allows(EmberFsmBlueprintAccess.CurrentTemplateId,()=>false));
        }
        [Test] public void InactiveProvidersAreNotInvoked()
        {
            int calls=0;
            EmberFsmObservationSources.Register("scope-test",()=>{calls++;return Array.Empty<EmberFsmObservedInstance>();},"missing-template-test");
            try { EmberFsmObservationSources.Read(null); Assert.AreEqual(0,calls); }
            finally { EmberFsmObservationSources.Remove("scope-test"); }
        }
        [Test] public void OldWindowRejectsGraphAndLayoutOperationsWithoutLosingGraph()
        {
            using var scope=EmberFsmBlueprintAccess.Register(EmberFsmBlueprintAccess.CurrentTemplateId,()=>true);
            var window=ScriptableObject.CreateInstance<EmberFsmBlueprintWindow>();
            var original=ScriptableObject.CreateInstance<EmberFsmGraph>();
            var other=ScriptableObject.CreateInstance<EmberFsmGraph>();
            try
            {
                window.SetGraph(original);
                typeof(EmberFsmBlueprintWindow).GetField("_templateScope",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(window,"another-template");
                Assert.IsFalse(window.IsTemplateAvailable);window.SetGraph(other);window.SaveLayout();
                Assert.AreSame(original,window.ParameterGraph);Assert.IsFalse(window.ApplyLayoutPreview());
                Assert.AreEqual("当前模板未启用此窗口。",window.PreviewLayout("all")[0]);
            }
            finally { UnityEngine.Object.DestroyImmediate(window); UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(other); }
        }
        [Test] public void ParameterExtensionsShareModuleAvailability()
        {
            bool enabled=true;
            EmberFsmBlueprintParameters.Register("scope-parameters",_=>{},ownerTemplateId:EmberFsmBlueprintAccess.CurrentTemplateId,enabled:()=>enabled);
            var window=ScriptableObject.CreateInstance<EmberFsmBlueprintWindow>();
            try
            {
                // Inspect the same predicate used by Draw; invoking every real template drawer outside OnGUI is invalid.
                var predicates = (System.Collections.Generic.Dictionary<string, Func<bool>>)typeof(EmberFsmBlueprintParameters)
                    .GetField("Availability", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                Assert.IsTrue(predicates["scope-parameters"]());
                enabled=false;Assert.IsFalse(predicates["scope-parameters"]());Assert.IsTrue(window.IsTemplateAvailable, "通用蓝图由正式扩展配置启用；业务提供者关闭只撤销自身入口。");
            }
            finally { UnityEngine.Object.DestroyImmediate(window);EmberFsmBlueprintParameters.Remove("scope-parameters"); }
        }
    }
}
