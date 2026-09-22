using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace Game.Narrative.Tests
{
    /// <summary>域重载后重新注册，保留本轮真实 NUnit 结果；仅在运行小说测试时写本地证据目录。</summary>
    [InitializeOnLoad]
    internal sealed class NarrativeTestReport : ICallbacks
    {
        #region 内部方法
        static NarrativeTestReport() => TestRunnerApi.RegisterTestCallback(new NarrativeTestReport());
        private static bool ContainsNarrative(ITestResultAdaptor result)
            => result.FullName.StartsWith("Game.Narrative.Tests", StringComparison.Ordinal) ||
               (result.HasChildren && result.Children.Any(ContainsNarrative));
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            if (!ContainsNarrative(result)) return;
            const string folder = ".utmp/visual-novel-m3";
            Directory.CreateDirectory(folder);
            TestRunnerApi.SaveResultToFile(result, folder + "/tests-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".xml");
        }
        #endregion
    }
}
