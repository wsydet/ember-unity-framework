using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    /// <summary>仅在维护验证显式设置 SessionState 路径后保存完整 NUnit 证据；域重载后继续注册。</summary>
    [InitializeOnLoad]
    internal sealed class PlayerTestEvidenceRecorder : ICallbacks
    {
        #region 内部参数
        private const string OUTPUT_KEY = "Game.Player.Tests.EvidenceOutput";
        private static readonly TestRunnerApi _api;
        private static readonly PlayerTestEvidenceRecorder _callbacks;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        static PlayerTestEvidenceRecorder()
        {
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new PlayerTestEvidenceRecorder();
            _api.RegisterCallbacks(_callbacks);
            AssemblyReloadEvents.beforeAssemblyReload += () => _api.UnregisterCallbacks(_callbacks);
        }
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            string relative = SessionState.GetString(OUTPUT_KEY, "");
            if (string.IsNullOrEmpty(relative)) return;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("测试证据必须保存于当前项目内。");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            TestRunnerApi.SaveResultToFile(result, path);
            SessionState.EraseString(OUTPUT_KEY);
        }
        #endregion
    }
}
