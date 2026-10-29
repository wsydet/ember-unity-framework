using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using Game.Player;
using Game.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.LevelAuthoring.Editor
{
    [InitializeOnLoad]
    public sealed class TopdownLevelAdapter:IEmberLevelAdapter
    {
        #region 内部参数
        internal static readonly TopdownLevelAdapter Instance=new();
        internal static bool Available=>EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelAuthoring)&&EmberProjectSetup.IsTemplateActive("topdown-2d")&&
            ((EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(PlayerModule),typeof(EmberModuleAttribute)))?.Enabled==true;
        public string Id=>"topdown.level";
        public string UnavailableReason=>Available?null:"当前正式模板或 Player 模块不支持俯视关卡。";
        public string TrialStatus=>TopdownLevelTrial.Status;
        public bool TrialReady=>TopdownLevelTrial.Ready;
        public bool IsCleaning=>TopdownLevelTrial.Cleaning;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static TopdownLevelAdapter()
        {
            EmberLevelAdapters.Register(Instance,"topdown-2d",()=>Available);TopdownObjectAdapter.Register();
            EmberConditionalMenu.Register("Ember/Level/俯视关卡工作台",()=>Available,Open);
        }
        internal static void RequireWrite()
        {if(!Available||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)throw new InvalidOperationException("俯视关卡制作需要所属模板、启用模块和空闲编辑状态。");}
        private EmberLevelDescriptor Describe(TopdownLevelDefinition definition)=>new(){id=definition.Id,displayName=definition.DisplayName,sceneGuid=AssetDatabase.AssetPathToGUID(definition.ScenePath),adapterId=Id,configuration=definition};
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open(){if(!Available)throw new InvalidOperationException("俯视关卡工作台不可用。");EmberLevelWorkbench.Open();}
        public IEnumerable<EmberLevelDescriptor> Discover()
        {
            if(!Available)yield break;
            foreach(var guid in AssetDatabase.FindAssets("t:TopdownLevelDefinition",new[]{"Assets/GameResource"}))
            {var item=AssetDatabase.LoadAssetAtPath<TopdownLevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));if(item!=null)yield return Describe(item);}
        }
        public IEnumerable<EmberLevelDiagnostic> Validate(EmberLevelDescriptor level,EmberLevelTrialRequest trial)
        {
            var errors=new List<EmberLevelDiagnostic>();
            void Error(string message)=>errors.Add(new EmberLevelDiagnostic("TD.level",message,EmberLevelSeverity.Error,level?.configuration));
            if(!Available){Error(UnavailableReason);return errors;}
            if(level?.configuration is not TopdownLevelDefinition definition){Error("缺少俯视关卡定义。");return errors;}
            if(level.id!=definition.Id||definition.ScenePath!=level.ScenePath||!File.Exists(level.ScenePath)||Discover().Count(l=>l.id==level.id)!=1)
            {Error("关卡路径、GUID或身份不唯一。");return errors;}
            try{using var capture=definition.Scheme.Capture();}catch(Exception e){Error(e.Message);}
            if(trial!=null&&(!float.IsFinite(trial.position.x)||!float.IsFinite(trial.position.y)))Error("试玩位置必须有限。");
            var preview=EditorSceneManager.OpenPreviewScene(level.ScenePath);
            try
            {
                var roots=preview.GetRootGameObjects();var players=roots.SelectMany(r=>r.GetComponentsInChildren<PlayerController>(true)).ToArray();
                var scopes=roots.SelectMany(r=>r.GetComponentsInChildren<GameSceneScope>(true)).ToArray();
                var identities=roots.SelectMany(r=>r.GetComponentsInChildren<TopdownLevelIdentity>(true)).ToArray();
                if(players.Length!=1||scopes.Length!=1||identities.Length!=1)Error("必须有唯一玩家、SceneScope 与关卡身份。");
                else if(identities[0].LevelId!=definition.Id||identities[0].ScenePath!=definition.ScenePath)Error("场景身份与关卡配置不匹配。");
                else if(identities[0].Configuration!=definition)Error("场景必须显式引用自己的关卡配置。");
                foreach(var item in roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
                    foreach(var d in TopdownObjectAdapter.Instance.Validate(item.gameObject))if(d.Severity==EmberLevelSeverity.Error)errors.Add(d);
                var actors=roots.SelectMany(r=>r.GetComponentsInChildren<Game.Entity.TopdownCombatActor>(true)).Select(a=>a.StableId).ToArray();
                var interactions=roots.SelectMany(r=>r.GetComponentsInChildren<TopdownInteraction>(true)).ToArray();
                if(actors.Distinct().Count()!=actors.Length||interactions.Select(i=>i.StableId).Distinct().Count()!=interactions.Length)Error("世界对象身份重复。");
                if(trial?.mode==EmberLevelStartMode.Checkpoint&&!interactions.Any(i=>i.IsCheckpoint&&i.StableId==trial.checkpointId))Error("检查点试玩必须指定已登记的检查点 ID。");
            }
            catch(Exception e){Error(e.Message);}
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            return errors;
        }
        public EmberLevelDescriptor Create(string folder,string name,EmberLevelDescriptor source)
        {
            RequireWrite();
            if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.Contains("..")||name.Contains("/")||name.Contains("\\"))throw new ArgumentException("新关名称无效。");
            folder=(folder??"").Replace('\\','/').TrimEnd('/');
            if(!folder.StartsWith("Assets/Game/Scenes",StringComparison.Ordinal)||folder!="Assets/Game/Scenes"&&!folder.StartsWith("Assets/Game/Scenes/",StringComparison.Ordinal)||!AssetDatabase.IsValidFolder(folder))throw new ArgumentException("新关必须写入已存在的 Assets/Game/Scenes 目录。");
            source??=Discover().FirstOrDefault();if(source?.configuration is not TopdownLevelDefinition original)throw new InvalidOperationException("缺少俯视起点关卡。");
            var schemePath=AssetDatabase.GetAssetPath(original.Scheme);
            var scenePath=folder+"/"+name+".unity";var configPath="Assets/GameResource/Resources/Config/Level/"+name+".asset";
            if(File.Exists(scenePath)||File.Exists(configPath))throw new InvalidOperationException("同名关卡或配置已存在。");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            if(!setup.Any(s=>s.isLoaded&&s.isActive&&!string.IsNullOrEmpty(s.path))||setup.Any(s=>s.isLoaded&&string.IsNullOrEmpty(s.path)))
                throw new InvalidOperationException("请先保存所有编辑场景，再创建关卡，以便完整恢复现场。");
            try
            {
                if(!AssetDatabase.CopyAsset(source.ScenePath,scenePath))throw new InvalidOperationException("复制场景失败。");
                var scene=EditorSceneManager.OpenScene(scenePath);var identity=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TopdownLevelIdentity>(true)).Single();
                var id="level."+Guid.NewGuid().ToString("N");using(var data=new SerializedObject(identity)){data.FindProperty("_levelId").stringValue=id;data.FindProperty("_scenePath").stringValue=scenePath;data.ApplyModifiedPropertiesWithoutUndo();}
                var definition=ScriptableObject.CreateInstance<TopdownLevelDefinition>();definition.Configure(id,name,scenePath,AssetDatabase.LoadAssetAtPath<PlayerScheme>(schemePath));AssetDatabase.CreateAsset(definition,configPath);
                using(var data=new SerializedObject(identity)){data.FindProperty("_configuration").objectReferenceValue=definition;data.ApplyModifiedPropertiesWithoutUndo();}
                EditorSceneManager.SaveScene(scene);
                var scenes=EditorBuildSettings.scenes.ToList();scenes.Add(new EditorBuildSettingsScene(scenePath,true));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssetIfDirty(definition);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            return Describe(AssetDatabase.LoadAssetAtPath<TopdownLevelDefinition>(configPath));
        }
        public void BeginTrial(EmberLevelTrialRequest frozen){EmberExtensionProject.Require(EmberExtensionProject.LevelTrial,true);if(!Available)throw new InvalidOperationException(UnavailableReason);TopdownLevelTrial.Begin(frozen);}
        public void StopTrial()=>TopdownLevelTrial.Stop();
        #endregion
    }
}
