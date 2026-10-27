using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.LevelAuthoring.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.State.Editor
{
    /// <summary>显式运行的失败验收；不安装常驻故障开关，不进入真实存档。</summary>
    [InitializeOnLoad]
    public static class PlatformerLevelFailureAcceptance
    {
        static PlatformerLevelFailureAcceptance()
        {
            _ = Game.Player.Editor.PlayerDevelopmentEnvironment.StorageRoot;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode || !SessionState.GetBool("Ember.Level.InjectStorageFailure",false)) return;
                SessionState.EraseBool("Ember.Level.InjectStorageFailure");
                string root = Game.Player.Editor.PlayerDevelopmentEnvironment.StorageRoot;
                string parent = System.IO.Path.GetFullPath("Library/PlayerFormalAcceptance") + System.IO.Path.DirectorySeparatorChar;
                if (string.IsNullOrEmpty(root) || !System.IO.Path.GetFullPath(root).StartsWith(parent,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("故障注入仅允许本次正式验收临时根。");
                System.IO.Directory.CreateDirectory(parent);
                // 独占CreateNew：不覆盖既有目录/证据；文件占据根路径使真实存储创建失败。
                using (var stream = new System.IO.FileStream(root,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write))
                    stream.WriteByte(1);
                SessionState.SetString("Ember.Level.InjectedStorageRoot",root);
            };
        }
        public static void ArmStorageFailure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EmberLevelTrialSession.Active) throw new InvalidOperationException("请先停止试玩。");
            SessionState.SetBool("Ember.Level.InjectStorageFailure",true);
        }
        public static async UniTask<string[]> VerifySkillRuntimeAsync()
        {
            if (!EmberLevelAdapters.Get(PlatformerLevelAuthoringAdapter.ADAPTER_ID).TrialReady) throw new InvalidOperationException("候选关尚未就绪。");
            var scene=Game.Player.PlayerModule.Instance.Controller.gameObject.scene;
            var roots=scene.GetRootGameObjects();
            var launch=roots.Single(g=>g.name=="Skill launch").GetComponent<Game.Entity.LaunchZone2D>();
            var layer=roots.Single(g=>g.name=="Skill background").GetComponent<Ember.LevelAuthoring.EmberParallaxLayer>();
            int triggered=0; Action<Game.Entity.ILaunchTarget2D> observed = _ => triggered++;
            launch.Launched+=observed;
            var a=new GameObject("acceptance camera A").AddComponent<Camera>(); a.enabled=false;
            var b=new GameObject("acceptance camera B").AddComponent<Camera>(); b.enabled=false;
            try
            {
                double until=Time.realtimeSinceStartupAsDouble+8;
                while(triggered==0 && Time.realtimeSinceStartupAsDouble<until) await UniTask.Yield();
                if(triggered==0)throw new Exception("未观察到真实物理触发弹射");
                a.transform.position=Vector3.zero; layer.BindRuntimeCamera(a);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                a.transform.position=new Vector3(10,5,0);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                var expected=layer.Evaluate(a.transform.position,Vector3.zero);
                if(Vector3.Distance(layer.transform.position,expected)>.001f)throw new Exception("视差位移不符");
                a.transform.position=Vector3.zero;
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                if(Vector3.Distance(layer.transform.position,layer.Baseline)>.001f)throw new Exception("视差往返漂移");
                b.transform.position=new Vector3(100,100,0);layer.BindRuntimeCamera(b);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                b.transform.position=new Vector3(110,105,0);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate); await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                if(Vector3.Distance(layer.transform.position,expected)>.001f)throw new Exception("切换相机基准错误");
                var renderer=layer.GetComponent<SpriteRenderer>();var decor=roots.Single(g=>g.name=="Skill decoration");
                if(renderer.drawMode!=SpriteDrawMode.Tiled || renderer.sortingOrder!=-90 || decor.GetComponent<Collider2D>()!=null)throw new Exception("运行渲染/碰撞声明不一致");
                return new[]{"physics launch observed="+triggered,"parallax outbound/return/camera-switch passed","tiled background sorting and non-colliding decoration passed"};
            }
            finally
            {
                launch.Launched-=observed;layer.BindRuntimeCamera(Ember.Camera.EmberCameraManager.Instance.MainCamera);
                UnityEngine.Object.Destroy(a.gameObject);UnityEngine.Object.Destroy(b.gameObject);
            }
        }
        private sealed class Adapter : IEmberLevelAdapter
        {
            public EmberLevelDescriptor Level;
            public bool Reject;
            public int Began, Stopped;
            public string Id => "acceptance.failure";
            public string UnavailableReason => null;
            public string TrialStatus => "injected";
            public bool TrialReady => false;
            public bool IsCleaning => false;
            public IEnumerable<EmberLevelDescriptor> Discover() { yield return Level; }
            public IEnumerable<EmberLevelDiagnostic> Validate(EmberLevelDescriptor level, EmberLevelTrialRequest trial)
            { if (Reject) yield return new EmberLevelDiagnostic("injected.preflight", "injected preflight rejection", EmberLevelSeverity.Error, level.configuration); }
            public EmberLevelDescriptor Create(string folder, string name, EmberLevelDescriptor source) => throw new NotSupportedException();
            public void BeginTrial(EmberLevelTrialRequest request) { Began++; throw new InvalidOperationException("injected partial preparation"); }
            public void StopTrial() { Stopped++; }
        }
        private sealed class ObjectAdapter : IEmberLevelObjectAdapter
        {
            public string Id => "acceptance.object-failure";
            public void ValidatePlacement(EmberLevelMaterial material, Scene scene) { }
            public void OnCreated(GameObject instance, bool duplicate)
            {
                var linked = new GameObject("injected related object"); SceneManager.MoveGameObjectToScene(linked,instance.scene);
                Undo.RegisterCreatedObjectUndo(linked,"related identity"); throw new InvalidOperationException("injected association failure");
            }
            public void BeforeDelete(GameObject instance) { throw new InvalidOperationException("injected unlink failure"); }
            public IEnumerable<EmberLevelDiagnostic> Validate(GameObject instance) { yield break; }
            public void DrawScene(GameObject instance) { }
        }
        public static string[] RunSynchronousFailures()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EmberLevelTrialSession.Active) throw new InvalidOperationException("请先停止试玩。");
            var original = EmberLevelAdapters.Get(PlatformerLevelAuthoringAdapter.ADAPTER_ID).Discover().First();
            var adapter = new Adapter { Level = new EmberLevelDescriptor { id=original.id, displayName=original.displayName, sceneGuid=original.sceneGuid, configuration=original.configuration, adapterId="acceptance.failure" } };
            var objectAdapter = new ObjectAdapter();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            bool options=EditorSettings.enterPlayModeOptionsEnabled;
            var results=new List<string>();
            EmberLevelAdapters.Register(adapter); EmberLevelAdapters.Register(objectAdapter);
            try
            {
                EditorSettings.enterPlayModeOptionsEnabled=false;
                adapter.Reject=true;
                try { EmberLevelTrialSession.Start(adapter.Level,EmberLevelStartMode.Entrance,Vector2.zero); throw new Exception("missing rejection"); }
                catch(InvalidOperationException error) { if(!error.Message.Contains("injected preflight")) throw; }
                if(adapter.Began!=0 || EmberLevelTrialSession.Active) throw new Exception("preflight started a session");
                results.Add("preflight failure: no session or BeginTrial"); adapter.Reject=false;
                try { EmberLevelTrialSession.Start(adapter.Level,EmberLevelStartMode.Entrance,Vector2.zero); throw new Exception("missing preparation failure"); }
                catch(InvalidOperationException error) { if(!error.Message.Contains("injected partial")) throw; }
                if(adapter.Stopped!=1 || EmberLevelTrialSession.Active || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("preparation leaked ownership");
                if(!EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isActive).SequenceEqual(setup.Select(s=>s.path+":"+s.isActive))) throw new Exception("scene restoration failed");
                if(!EmberLevelTrialSession.LastError.Contains("injected partial")) throw new Exception("failure diagnostic lost");
                results.Add("partial preparation: cleanup requested, scene setup restored, error retained");
                var scene=EditorSceneManager.NewPreviewScene(); var material=ScriptableObject.CreateInstance<EmberLevelMaterial>();
                try
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlatformerLevelSamples.ROOT+"/LaunchPad.prefab");
                    material.Configure("failure","failure",EmberLevelMaterialKind.Mechanism,prefab,null,null,objectAdapter.Id,hasPhysics:true);
                    int before=scene.rootCount;
                    try { EmberLevelObjectEditing.Place(material,scene,Vector3.zero,Quaternion.identity,Vector3.one); throw new Exception("missing creation failure"); }
                    catch(InvalidOperationException error) { if(!error.Message.Contains("association failure"))throw; }
                    if(scene.rootCount!=before)throw new Exception("partial object/association leaked");
                    var target=new GameObject("retained"); SceneManager.MoveGameObjectToScene(target,scene);
                    try { EmberLevelObjectEditing.Delete(target,objectAdapter);throw new Exception("missing deletion failure"); }
                    catch(InvalidOperationException error) { if(!error.Message.Contains("unlink failure"))throw; }
                    if(target==null)throw new Exception("failed unlink deleted target");
                    results.Add("object association failure: atomic rollback; failed unlink retains object");
                }
                finally { UnityEngine.Object.DestroyImmediate(material);EditorSceneManager.ClosePreviewScene(scene); }
                return results.ToArray();
            }
            finally { EditorSettings.enterPlayModeOptionsEnabled=options;EmberLevelAdapters.Unregister(adapter);EmberLevelAdapters.Unregister(objectAdapter);EmberLevelTrialSession.ReportFailure(null); }
        }
    }
}
