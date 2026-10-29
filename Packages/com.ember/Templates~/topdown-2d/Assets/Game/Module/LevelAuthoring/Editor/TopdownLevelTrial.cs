using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using Game.Module;
using Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.LevelAuthoring.Editor
{
    [InitializeOnLoad]
    internal static class TopdownLevelTrial
    {
        #region 内部参数
        internal static string Status {get;private set;}="编辑";
        internal static bool Ready {get;private set;}
        internal static bool Cleaning {get;private set;}
        private static bool _requested;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static TopdownLevelTrial()
        {
            EditorApplication.playModeStateChanged+=state=>
            {if(state==PlayModeStateChange.EnteredEditMode){PlayerTrialBridge.Clear();Ready=false;_requested=false;}};
            var request=EmberLevelTrialSession.Request;
            if(request?.adapterId=="topdown.level"&&EditorApplication.isPlayingOrWillChangePlaymode)
                try{Configure(request);}catch(Exception e){Fail(e);EditorApplication.delayCall+=Stop;}
        }
        private static void Fail(Exception e){Status="试玩失败："+e.Message;EmberLevelTrialSession.ReportFailure(Status);}
        private static void Configure(EmberLevelTrialRequest request)
        {
            EmberExtensionProject.Require(EmberExtensionProject.LevelTrial);
            if(!TopdownLevelAdapter.Available)throw new InvalidOperationException("试玩模板或模块已失效。");
            var configPath=AssetDatabase.GUIDToAssetPath(request.configurationGuid);var config=AssetDatabase.LoadAssetAtPath<TopdownLevelDefinition>(configPath);
            if(config==null||config.Id!=request.levelId||AssetDatabase.AssetPathToGUID(config.ScenePath)!=request.sceneGuid||
                AssetDatabase.GetAssetDependencyHash(configPath).ToString()!=request.configurationHash)throw new InvalidOperationException("冻结的关卡身份或配置已改变，请重新发起试玩。");
            Vector2? position=request.mode==EmberLevelStartMode.Position?request.position:null;
            if(request.mode==EmberLevelStartMode.Checkpoint)
            {
                var preview=EditorSceneManager.OpenPreviewScene(config.ScenePath);
                try{position=preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TopdownInteraction>(true)).Single(i=>i.IsCheckpoint&&i.StableId==request.checkpointId).transform.position;}
                finally{EditorSceneManager.ClosePreviewScene(preview);}
            }
            var storage=ScriptableObject.CreateInstance<PlayerDataConfig>();storage.hideFlags=HideFlags.HideAndDontSave;
            storage.Configure("Progress","ember.topdown2d.progress",new[]{0});
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/TopdownTrials",request.requestId));
            PlayerTrialBridge.Configure(config.ScenePath,config.Scheme,position,storage,root);
            PlayerTrialBridge.StartGameplay=()=>
            {
                if(_requested)return;_requested=true;PlayerTrialBridge.AutoEnterPending=false;StartAfterIdle().Forget();
            };
            Status="经 Framework 正式启动 · 开发存档："+root;Ready=false;
        }
        private static async UniTask Observe(EmberTransitionOperation operation)
        {
            try
            {
                var result=await operation.WhenInteractive;await UniTask.SwitchToMainThread();
                if(result.Status!=EmberTransitionStatus.Completed)throw new InvalidOperationException(result.Status+" "+result.Error);
                Ready=PlayerModule.Instance.HasSession;Status=Ready?"试玩可交互 · 存档与正式进度隔离":"试玩未交接玩家";
            }
            catch(Exception e){Fail(e);}
        }
        private static async UniTask StartAfterIdle()
        {
            try
            {
                await UniTask.WaitUntil(()=>!EditorApplication.isPlaying||!PlayerTrialBridge.Active||!GameLauncher.Instance.Fsm.IsTransitioning);
                if(!EditorApplication.isPlaying||!PlayerTrialBridge.Active)return;
                EmberExtensionProject.Require(EmberExtensionProject.LevelTrial);
                await Observe(GameLauncher.Instance.Fsm.RequestTransitionTo<GameplayState>());
            }
            catch(Exception e){Fail(e);}
        }
        private static async UniTask StopAsync()
        {
            Cleaning=true;Ready=false;Status="等待试玩运行与已接受 I/O 停止…";
            try{if(GameLauncher.CurrentLifetime!=null)await GameLauncher.Instance.ShutdownFrameworkAsync();}
            catch(Exception e){Fail(e);}
            finally{await UniTask.SwitchToMainThread();Cleaning=false;EditorApplication.isPlaying=false;}
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Begin(EmberLevelTrialRequest request)
        {EmberExtensionProject.Require(EmberExtensionProject.LevelTrial,true);TopdownLevelAdapter.RequireWrite();_requested=false;Configure(request);EditorApplication.isPlaying=true;}
        internal static void Stop()
        {
            if(Cleaning)return;
            if(!EditorApplication.isPlaying){PlayerTrialBridge.Clear();Ready=false;EditorApplication.isPlaying=false;return;}
            StopAsync().Forget();
        }
        #endregion
    }
}
