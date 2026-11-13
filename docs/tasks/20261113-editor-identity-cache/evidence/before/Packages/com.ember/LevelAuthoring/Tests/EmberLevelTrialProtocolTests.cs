using System;
using System.Collections.Generic;
using System.Reflection;
using Ember.LevelAuthoring.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.LevelAuthoring.Tests
{
    /// <summary>不进入 Play、不切模板；使用纯协议适配器核验冻结身份、所有权和停止恢复。</summary>
    public sealed class EmberLevelTrialProtocolTests
    {
        #region 内部参数
        private const string Key = "Ember.LevelAuthoring.Trial";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;
        private string _folder, _error;
        private Adapter _adapter;
        private UnityEngine.Object _selection;
        private sealed class Adapter : IEmberLevelAdapter
        {
            public string Id => "test.trial-protocol";
            public string UnavailableReason => null;
            public EmberLevelDescriptor level;
            public bool failStop;
            public int stops;
            public IEnumerable<EmberLevelDescriptor> Discover() { yield return level; }
            public IEnumerable<EmberLevelDiagnostic> Validate(EmberLevelDescriptor descriptor, EmberLevelTrialRequest request) { yield break; }
            public EmberLevelDescriptor Create(string folder, string name, EmberLevelDescriptor source) => throw new NotSupportedException();
            public void BeginTrial(EmberLevelTrialRequest frozen) => throw new NotSupportedException();
            public string TrialStatus => "fixture";
            public bool TrialReady => false;
            public bool IsCleaning => false;
            public void StopTrial() { stops++; if (failStop) throw new InvalidOperationException("injected-stop"); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void Setup()
        {
            if (EmberLevelTrialSession.Active || EditorApplication.isPlayingOrWillChangePlaymode
                || !string.IsNullOrEmpty(SessionState.GetString(Key + ".Retry", ""))) Assert.Ignore("用户试玩会话正在进行。");
            _error = SessionState.GetString(Key + ".Error", ""); _selection = Selection.activeObject;
            _folder = "Assets/EmberTrialProtocol-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", _folder.Substring(7));
            var config = ScriptableObject.CreateInstance<EmberLevelMaterial>();
            AssetDatabase.CreateAsset(config, _folder + "/Config.asset");
            _adapter = new Adapter { level = new EmberLevelDescriptor { id = "fixture", adapterId = "test.trial-protocol",
                sceneGuid = "fixture-scene", configuration = config } };
            EmberLevelAdapters.Register(_adapter, "", () => true);
        }
        [TearDown] public void Teardown()
        {
            if (_adapter == null) return;
            typeof(EmberLevelTrialSession).GetField("_state", Private).SetValue(null, null);
            EmberLevelAdapters.Unregister(_adapter);
            SessionState.EraseString(Key); SessionState.EraseString(Key + ".Retry"); SessionState.EraseBool("Ember.LevelAuthoring.OwnsSceneRestore");
            EmberLevelTrialSession.ReportFailure(_error); Selection.activeObject = _selection;
            AssetDatabase.DeleteAsset(_folder);
        }
        [Test] public void FrozenRetryRejectsReboundConfigurationAndChangedSource()
        {
            var request = Request();
            Assert.AreSame(_adapter.level, Resolve(request));
            var second = ScriptableObject.CreateInstance<EmberLevelMaterial>();
            AssetDatabase.CreateAsset(second, _folder + "/Second.asset");
            var first = _adapter.level.configuration; _adapter.level.configuration = second;
            var rebound = Assert.Throws<TargetInvocationException>(() => Resolve(request));
            StringAssert.Contains("换绑", rebound.InnerException.Message);
            _adapter.level.configuration = first;
            var serialized = new SerializedObject(first);
            serialized.FindProperty("_description").stringValue = "changed frozen configuration";
            serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(first);
            var changed = Assert.Throws<TargetInvocationException>(() => Resolve(request));
            StringAssert.Contains("冻结配置", changed.InnerException.Message);
        }
        [Test] public void RequestCopiesAndLateCallbacksCannotChangeCurrentOwner()
        {
            InstallState(Request());
            var copy = EmberLevelTrialSession.Request; copy.requestId = "tampered";
            Assert.AreEqual("current", EmberLevelTrialSession.Request.requestId);
            EmberLevelTrialSession.ReportFailure("current", "current-error");
            EmberLevelTrialSession.ReportFailure("old", "late-error");
            EmberLevelTrialSession.Stop("old");
            EmberLevelTrialSession.Retry("old");
            Assert.AreEqual("", SessionState.GetString(Key + ".Retry", ""));
            Assert.AreEqual("current-error", EmberLevelTrialSession.LastError);
            Assert.AreEqual(0, _adapter.stops); Assert.IsTrue(EmberLevelTrialSession.Active);
        }
        [Test] public void StopExceptionCancelsRetryAndQueuesEditorRestore()
        {
            InstallState(Request()); _adapter.failStop = true;
            SessionState.SetString(Key + ".Retry", "pending");
            Assert.DoesNotThrow(() => EmberLevelTrialSession.Stop("current"));
            Assert.AreEqual(1, _adapter.stops); StringAssert.Contains("injected-stop", EmberLevelTrialSession.LastError);
            Assert.AreEqual("", SessionState.GetString(Key + ".Retry", ""));
            // The EditMode runner owns its temporary unsaved scene; restoring it is not a legal Start precondition.
            // Actual saved-editor snapshot restoration is checked separately outside the test runner.
            var state = typeof(EmberLevelTrialSession).GetField("_state", Private).GetValue(null);
            Assert.IsTrue((bool)state.GetType().GetField("returning").GetValue(state));
            Assert.IsTrue(SessionState.GetBool("Ember.LevelAuthoring.OwnsSceneRestore", false));
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private EmberLevelTrialRequest Request() => new EmberLevelTrialRequest { requestId = "current", adapterId = _adapter.Id,
            levelId = _adapter.level.id, sceneGuid = _adapter.level.sceneGuid,
            configurationGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_adapter.level.configuration)),
            configurationHash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(_adapter.level.configuration)).ToString() };
        private object Resolve(EmberLevelTrialRequest request) => typeof(EmberLevelTrialSession).GetMethod("ResolveRetry", Private).Invoke(null, new object[] { _adapter, request });
        private static void InstallState(EmberLevelTrialRequest request)
        {
            var type = typeof(EmberLevelTrialSession).GetNestedType("Snapshot", BindingFlags.NonPublic);
            var state = Activator.CreateInstance(type, true);
            type.GetField("request").SetValue(state, request);
            type.GetField("scenes").SetValue(state, EditorSceneManager.GetSceneManagerSetup());
            type.GetField("selection").SetValue(state, "");
            var view = SceneView.lastActiveSceneView;
            type.GetField("pivot").SetValue(state, view != null ? view.pivot : Vector3.zero);
            type.GetField("rotation").SetValue(state, view != null ? view.rotation : Quaternion.identity);
            type.GetField("size").SetValue(state, view != null ? view.size : 10f);
            type.GetField("mode2D").SetValue(state, view != null && view.in2DMode);
            type.GetField("orthographic").SetValue(state, view != null && view.orthographic);
            typeof(EmberLevelTrialSession).GetField("_state", Private).SetValue(null, state);
            SessionState.SetBool("Ember.LevelAuthoring.OwnsSceneRestore", true);
        }
        #endregion
    }
}
