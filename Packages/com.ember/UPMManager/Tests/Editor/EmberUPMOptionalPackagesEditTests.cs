using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberUPMOptionalPackagesEditTests
    {
        [TestCase("com.borodar.rainbow-folders", "rainbow-folders-v2.4.5")]
        [TestCase("com.borodar.rainbow-hierarchy", "rainbow-hierarchy-v2.6.5")]
        [TestCase("com.ryanindiedev.inputdevicedetector", "inputdevicedetector-v1.0.0")]
        [TestCase("com.flyingworm.consolepro", "consolepro-v3.9.81")]
        public void InstallAddress_UsesThePackageSpecificPublishedTag(string name, string tag)
        {
            var definition = EmberUPMOptionalPackages.All.Single(p => p.Name == name);
            Assert.AreEqual("https://github.com/wsydet/ember-thirdparty-upm.git?path=/" + name + "#" + tag,
                definition.InstallUrl);
        }

        [Test]
        public void RegisteredPackage_ReportsActualVersionAndBlocksReplacingIt()
        {
            var state = EmberUPMOptionalPackages.FromEvidence(true, "9.9.9", "UPM · 本地包", true, true);
            StringAssert.Contains("v9.9.9", state.Text);
            StringAssert.Contains("本地包", state.Text);
            Assert.IsTrue(state.Installed);
            Assert.IsFalse(state.CanInstall);
        }

        [Test]
        public void MissingPackage_WithAnAlreadyLoadedPluginTypeDoesNotOfferDuplicateInstall()
        {
            var definition = new EmberUPMOptionalPackages.Definition("com.ember.nonexistent-test-plugin",
                "Probe", typeof(EmberUPMOptionalPackagesEditTests).FullName, "", "1.0.0", "test-v1.0.0");
            var state = EmberUPMOptionalPackages.Inspect(definition, new PackageInfo[0]);
            Assert.IsTrue(state.Installed);
            Assert.IsFalse(state.CanInstall);
            StringAssert.Contains("直接导入", state.Text);
            StringAssert.Contains("版本未知", state.Text);
        }

        [Test]
        public void UncompiledAssets_AreNotMisreportedAsMissingOrSuccessfullyInstalled()
        {
            var pending = EmberUPMOptionalPackages.FromEvidence(false, null, null, false, true);
            Assert.IsFalse(pending.Installed);
            Assert.IsFalse(pending.CanInstall);
            StringAssert.Contains("待编译确认", pending.Text);
            var missing = EmberUPMOptionalPackages.FromEvidence(false, null, null, false, false);
            Assert.IsFalse(missing.Installed);
            Assert.IsTrue(missing.CanInstall);
            StringAssert.Contains("未安装", missing.Text);
        }

        [Test]
        public void PackageRegistration_RequestsRefreshWithoutChangingTheCurrentLayoutSnapshot()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var manager = ScriptableObject.CreateInstance<EmberUPMManager>();
            try
            {
                var states = new Dictionary<string, EmberUPMOptionalPackages.State>();
                var type = typeof(EmberUPMManager);
                type.GetField("_optionalPackageStates", flags).SetValue(manager, states);
                type.GetField("_optionalPackagesDirty", flags).SetValue(manager, false);
                type.GetMethod("OnPackagesRegistered", flags).Invoke(manager, new object[] { null });
                Assert.IsTrue((bool)type.GetField("_optionalPackagesDirty", flags).GetValue(manager));
                Assert.AreSame(states, type.GetField("_optionalPackageStates", flags).GetValue(manager));
            }
            finally { Object.DestroyImmediate(manager); }
        }
    }
}
