using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEditor.PackageManager;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberUPMOptionalPackagesEditTests
    {
        private static EmberUPMOptionalPackages.Definition Mcp =>
            EmberUPMOptionalPackages.All.Single(p => p.Name == "com.coplaydev.unity-mcp");
        private const string OfficialUrl = "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity";

        [TestCase("10.2.0", true, "有推荐更新")]
        [TestCase("9.9.0", true, "有推荐更新")]
        [TestCase("10.3.0", false, "已达推荐版本")]
        [TestCase("10.10.0", false, "不降级")]
        [TestCase("11.0.0", false, "不降级")]
        [TestCase("10.3.0-pre.1", false, "人工确认")]
        [TestCase("unknown", false, "人工确认")]
        [TestCase("10.2", false, "人工确认")]
        [TestCase(null, false, "人工确认")]
        public void Mcp_ComparesResolvedVersionWithoutDowngrading(string version, bool upgrade, string status)
        {
            var state = EmberUPMOptionalPackages.InspectRegistered(Mcp, version, PackageSource.Git,
                Mcp.Name + "@" + OfficialUrl + "#main", true);
            Assert.AreEqual(upgrade, state.CanUpgrade);
            Assert.IsTrue(state.Installed);
            Assert.IsFalse(state.CanInstall);
            StringAssert.Contains(status, state.Text);
            StringAssert.Contains("框架推荐 v10.3.0", state.Text);
        }

        [TestCase(PackageSource.Local)]
        [TestCase(PackageSource.Embedded)]
        [TestCase(PackageSource.Registry)]
        public void Mcp_LocalAndCustomSourcesRemainProtected(PackageSource source)
        {
            var state = EmberUPMOptionalPackages.InspectRegistered(Mcp, "10.2.0", source,
                Mcp.Name + "@" + OfficialUrl + "#v10.2.0", true);
            Assert.IsFalse(state.CanUpgrade);
            StringAssert.Contains("保留", state.Text);
            StringAssert.Contains("有推荐更新", state.Text);
        }

        [TestCase("", true)]
        [TestCase("#main", true)]
        [TestCase("#v10.2.0", true)]
        [TestCase("#v10.2.0-custom", false)]
        [TestCase("#my-patch", false)]
        [TestCase("#aa5fc638d623f56178d50329d9ad8c541a57fe66", false)]
        public void Mcp_CustomRefsRequireManualManagement(string revision, bool supported)
            => Assert.AreEqual(supported, EmberUPMOptionalPackages.IsSupportedMcpUrl(OfficialUrl + revision));

        [TestCase("https://github.com/someone/unity-mcp.git?path=/MCPForUnity#v10.2.0")]
        [TestCase("https://github.com.evil.test/CoplayDev/unity-mcp.git?path=/MCPForUnity")]
        [TestCase("https://github.com/CoplayDev/unity-mcp.git?path=/Custom")]
        [TestCase("https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity&custom=true")]
        [TestCase("https://user@github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity")]
        [TestCase("file:../unity-mcp")]
        [TestCase("git@github.com:CoplayDev/unity-mcp.git?path=/MCPForUnity")]
        public void Mcp_UnmanagedAddressesNeverOfferUpgrade(string url)
        {
            var state = EmberUPMOptionalPackages.InspectRegistered(Mcp, "10.2.0", PackageSource.Git,
                Mcp.Name + "@" + url, true);
            Assert.IsFalse(state.CanUpgrade);
            StringAssert.Contains("保留", state.Text);
        }

        [Test]
        public void Mcp_IndirectDependencyDoesNotBecomeADirectOverride()
        {
            Assert.IsFalse(EmberUPMOptionalPackages.InspectRegistered(Mcp, "10.2.0", PackageSource.Git,
                Mcp.Name + "@" + OfficialUrl + "#v10.2.0", false).CanUpgrade);
        }

        [Test]
        public void Upgrade_RevalidatesAndDispatchesOnlyThePinnedRecommendedUrl()
        {
            string url = OfficialUrl + "#v10.2.0";
            var info = MakePackage("10.2.0", url);
            int calls = 0;
            string requested = null;
            Assert.IsTrue(EmberUPMOptionalPackages.TryUpgrade(Mcp, new[] { info }, Manifest(url),
                value => { calls++; requested = value; }, out var error), error);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(Mcp.InstallUrl, requested);
        }

        [TestCase("file:../local-mcp")]
        [TestCase("https://github.com/someone/unity-mcp.git?path=/MCPForUnity#main")]
        [TestCase("https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#my-patch")]
        [TestCase("https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0")]
        public void Upgrade_StaleResolvedStateCannotReplaceAChangedManifest(string changedUrl)
        {
            AssertUpgradeBlocked(new[] { MakePackage("10.2.0", OfficialUrl + "#v10.2.0") }, Manifest(changedUrl));
        }

        [Test]
        public void Upgrade_MissingAmbiguousOrUnresolvedEvidenceCannotDispatch()
        {
            string url = OfficialUrl + "#v10.2.0";
            var packages = new[] { MakePackage("10.2.0", url) };
            AssertUpgradeBlocked(packages, "{}");
            AssertUpgradeBlocked(packages, "{\"other\":{\"" + Mcp.Name + "\":\"" + url + "\"}}");
            AssertUpgradeBlocked(packages, "{\"dependencies\":{\"" + Mcp.Name + "\":\"" + url
                + "\",\"" + Mcp.Name + "\":\"file:../local\"}}");
            AssertUpgradeBlocked(null, Manifest(url));
            AssertUpgradeBlocked(new PackageInfo[0], Manifest(url));
            AssertUpgradeBlocked(new[] { MakePackage("10.3.0", OfficialUrl + "#v10.3.0") }, Manifest(url));
            AssertUpgradeBlocked(new[] { MakePackage("10.2.0", "file:../local", PackageSource.Local) }, Manifest(url));
        }

        private static PackageInfo MakePackage(string version, string url, PackageSource source = PackageSource.Git)
        {
            // PackageInfo has no public setters; use the same serialized fields exposed by this Unity Editor.
            return JsonUtility.FromJson<PackageInfo>("{\"m_Name\":\"" + Mcp.Name + "\",\"m_Version\":\""
                + version + "\",\"m_PackageId\":\"" + Mcp.Name + "@" + url
                + "\",\"m_Source\":" + (int)source + ",\"m_IsDirectDependency\":true}");
        }

        [Test]
        public void Manifest_EscapedDuplicateOrMalformedDeclarationsAreNotGuessed()
        {
            string url = OfficialUrl + "#v10.2.0";
            var packages = new[] { MakePackage("10.2.0", url) };
            AssertUpgradeBlocked(packages, "{\"dependencies\":{\"" + Mcp.Name + "\":\"" + url
                + "\",\"com.coplaydev.unity-\\u006dcp\":\"file:../local\"}}");
            AssertUpgradeBlocked(packages, "{\"dependencies\":{\"" + Mcp.Name + "\":\"" + url + "\",}}");
            Assert.IsTrue(EmberUPMOptionalPackages.TryReadManifestDependency(Mcp,
                "{\"dependencies\":{\"com.unity.test-framework\":\"1.7.0\"}}", out var missing));
            Assert.IsNull(missing);
            Assert.IsTrue(EmberUPMOptionalPackages.TryReadManifestDependency(Mcp,
                Manifest("file:../local"), out var local));
            Assert.AreEqual("file:../local", local);
        }

        private static string Manifest(string url) => "{\"dependencies\":{\"" + Mcp.Name + "\":\"" + url + "\"}}";

        private static void AssertUpgradeBlocked(PackageInfo[] packages, string manifest)
        {
            int calls = 0;
            Assert.IsFalse(EmberUPMOptionalPackages.TryUpgrade(Mcp, packages, manifest, _ => calls++, out var error));
            Assert.AreEqual(0, calls);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void UnityMcp_UsesItsOfficialRepositoryAndPinnedPackageDirectory()
        {
            var definition = EmberUPMOptionalPackages.All.Single(p => p.Name == "com.coplaydev.unity-mcp");
            var uri = new System.Uri(definition.InstallUrl);
            Assert.AreEqual("https", uri.Scheme);
            Assert.AreEqual("github.com", uri.Host);
            Assert.AreEqual("/CoplayDev/unity-mcp.git", uri.AbsolutePath);
            Assert.AreEqual("?path=/MCPForUnity", uri.Query);
            Assert.AreEqual("#v10.3.0", uri.Fragment);
            Assert.AreEqual("10.3.0", definition.Version);
            Assert.AreEqual("MCPForUnity.Editor.Windows.MCPForUnityEditorWindow", definition.TypeName);
        }

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
