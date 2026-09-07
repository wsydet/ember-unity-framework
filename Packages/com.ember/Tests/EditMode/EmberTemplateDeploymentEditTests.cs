// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Core.Editor;

using NUnit.Framework;

using UnityEditor;
using UnityEngine;

namespace Ember.UI.Tests
{
    /// <summary>消费端活动模板与旧部署记录迁移规则测试。</summary>
    public class EmberTemplateDeploymentEditTests
    {
        #region 外部方法

        [Test]
        public void ActiveDeployment_ExplicitIdShouldResolveMatchingRecord()
        {
            var data = CreateData("platformer2d", "base", "platformer2d");

            var active = EmberProjectSetup.ResolveActiveDeployment(data);

            Assert.IsNotNull(active);
            Assert.AreEqual("platformer2d", active.templateId);
        }

        [Test]
        public void ActiveDeployment_LegacySingleRecordShouldResolveWithoutMutation()
        {
            var data = CreateData(null, "base");

            var active = EmberProjectSetup.ResolveActiveDeployment(data);

            Assert.IsNotNull(active);
            Assert.AreEqual("base", active.templateId);
            Assert.IsNull(data.activeTemplateId);
        }

        [Test]
        public void ActiveDeployment_LegacyMultipleRecordsShouldRemainAmbiguous()
        {
            var data = CreateData(null, "base", "platformer2d");

            Assert.IsNull(EmberProjectSetup.ResolveActiveDeployment(data));
            StringAssert.Contains(
                "明确认定当前活动模板",
                EmberProjectSetup.GetDeploymentBlockReason(data, "base"));
        }

        [Test]
        public void DeploymentGate_FirstDeploymentShouldAllowAnyTemplate()
        {
            Assert.IsNull(EmberProjectSetup.GetDeploymentBlockReason(
                new DeployedTemplatesData(),
                "platformer2d"));
        }

        [Test]
        public void DeploymentGate_CurrentActiveTemplateShouldAllowRepair()
        {
            var data = CreateData("base", "base", "platformer2d");

            Assert.IsNull(EmberProjectSetup.GetDeploymentBlockReason(data, "base"));
        }

        [Test]
        public void DeploymentGate_DifferentTemplateShouldRequireMigration()
        {
            var data = CreateData("base", "base", "platformer2d");

            var reason = EmberProjectSetup.GetDeploymentBlockReason(data, "platformer2d");

            StringAssert.Contains("需要迁移", reason);
        }

        [Test]
        public void DeploymentGate_BrokenExplicitActiveIdShouldBlock()
        {
            var data = CreateData("missing", "base");

            var reason = EmberProjectSetup.GetDeploymentBlockReason(data, "base");

            StringAssert.Contains("没有对应记录", reason);
        }

        [Test]
        public void DeploymentJson_LegacyFormatShouldRemainReadable()
        {
            const string json =
                "{\"records\":[{\"templateId\":\"base\",\"version\":\"0.5.0\"}]}";

            var data = JsonUtility.FromJson<DeployedTemplatesData>(json);
            var active = EmberProjectSetup.ResolveActiveDeployment(data);

            Assert.IsNull(data.activeTemplateId);
            Assert.AreEqual("base", active.templateId);
        }

        [Test]
        public void DeploymentJson_ActiveTemplateIdShouldRoundTrip()
        {
            var source = CreateData("base", "base");

            var roundTrip = JsonUtility.FromJson<DeployedTemplatesData>(
                JsonUtility.ToJson(source));

            Assert.AreEqual("base", roundTrip.activeTemplateId);
            Assert.AreEqual("base", EmberProjectSetup.ResolveActiveDeployment(roundTrip).templateId);
        }

        [Test]
        public void UnifiedSetupWindow_ShouldOwnOnlyProjectCenterMenuEntry()
        {
            var setupMethod = typeof(EmberSetupWindow).GetMethod(
                nameof(EmberSetupWindow.ShowWindow));
            var setupMenus = setupMethod.GetCustomAttributes(typeof(MenuItem), false);
            Assert.AreEqual(1, setupMenus.Length);
            Assert.AreEqual("Ember/项目中心", ((MenuItem)setupMenus[0]).menuItem);

            var legacyType = typeof(EmberSetupWindow).Assembly.GetType(
                "Ember.Core.Editor.EmberTemplateEditorWindow");
            var legacyMethod = legacyType.GetMethod("ShowWindow");
            Assert.AreEqual(0, legacyMethod.GetCustomAttributes(typeof(MenuItem), false).Length);
            var legacyValidationMethod = typeof(EmberProjectSetup).GetMethod(
                nameof(EmberProjectSetup.ValidateGeneratedFiles));
            Assert.IsNotNull(legacyValidationMethod);
            Assert.AreEqual(0, legacyValidationMethod.GetCustomAttributes(typeof(MenuItem), false).Length);
            Assert.IsNotNull(typeof(EmberSetupWindow).Assembly.GetType(
                "Ember.Core.Editor.EmberProjectSetupPanel"));
            Assert.IsNotNull(typeof(EmberSetupWindow).Assembly.GetType(
                "Ember.Core.Editor.EmberTemplateDevelopmentPanel"));
        }

        [TestCase(null, "source3d-2p5d", false)]
        [TestCase("", "source3d-2p5d", false)]
        [TestCase("base", "source3d-2p5d", false)]
        [TestCase("source3d-2p5d", "source3d-2p5d", true)]
        public void TemplateSync_AutoReloadShouldOnlyTargetActiveEditingCopy(
            string editingTemplateId,
            string syncedTemplateId,
            bool expected)
        {
            var editing = editingTemplateId == null
                ? null
                : new EditingTemplateRecord { templateId = editingTemplateId };

            Assert.AreEqual(
                expected,
                EmberTemplateDevelopmentPanel.ShouldAutoReloadEditingCopyAfterSync(
                    editing,
                    syncedTemplateId));
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static DeployedTemplatesData CreateData(
            string activeTemplateId,
            params string[] templateIds)
        {
            var data = new DeployedTemplatesData { activeTemplateId = activeTemplateId };
            foreach (var templateId in templateIds)
            {
                data.records.Add(new DeployedTemplateRecord
                {
                    templateId = templateId,
                    version = "0.1.0",
                    frameworkVersion = "0.11.0"
                });
            }
            return data;
        }

        #endregion
    }
}
