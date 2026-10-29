using System;
using System.IO;
using System.Linq;
using Ember.LevelAuthoring.Editor;
using Game.Entity;
using Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.LevelAuthoring.Editor
{
    public static class TopdownLevelExampleAssets
    {
        #region 内部方法
        private static void Material(string name,string label,string prefab,bool physics)
        {
            var item=ScriptableObject.CreateInstance<EmberLevelMaterial>();
            item.Configure("topdown."+name,label,EmberLevelMaterialKind.Mechanism,AssetDatabase.LoadAssetAtPath<GameObject>(prefab),null,null,
                "topdown.object",hasPhysics:physics,description:"俯视样例；放置或复制会生成本关唯一持久身份。");
            AssetDatabase.CreateAsset(item,"Assets/Game/Module/LevelAuthoring/Editor/Materials/"+name+".asset");
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Create()
        {
            TopdownLevelAdapter.RequireWrite();const string folder="Assets/GameResource/Resources/Config/Level";
            if(Directory.Exists(folder))throw new InvalidOperationException("关卡配置目录已存在，禁止重复制作覆盖。");
            Directory.CreateDirectory(folder);Directory.CreateDirectory("Assets/Game/Module/LevelAuthoring/Editor/Materials");AssetDatabase.Refresh();
            var definition=ScriptableObject.CreateInstance<TopdownLevelDefinition>();
            definition.Configure("starter","俯视训练场","Assets/Game/Scenes/GameplayScene.unity",Resources.Load<PlayerScheme>("Config/Player/PlayerScheme"));
            AssetDatabase.CreateAsset(definition,folder+"/Starter.asset");
            AssetDatabase.SaveAssets();
            TopdownLevelAdapter.Instance.Create("Assets/Game/Scenes","TopdownArena",TopdownLevelAdapter.Instance.Discover().Single(l=>l.id=="starter"));
            CompleteArena();
        }
        /// <summary>续接首次制作中已复制但未完成登记的本轮 Arena；不替换其他关卡或既有素材。</summary>
        public static void CompleteArena()
        {
            TopdownLevelAdapter.RequireWrite();
            if(File.Exists("Assets/GameResource/Player/Topdown/SolidObstacle.prefab"))throw new InvalidOperationException("Arena 制作已经完成，禁止重复覆盖。");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                const string scenePath="Assets/Game/Scenes/TopdownArena.unity";
                var scene=EditorSceneManager.OpenScene(scenePath);var roots=scene.GetRootGameObjects();
                if(!File.Exists("Assets/GameResource/Resources/Config/Level/TopdownArena.asset"))
                {
                    var identity=roots.SelectMany(r=>r.GetComponentsInChildren<TopdownLevelIdentity>(true)).Single();
                    if(identity.ScenePath!=scenePath||!identity.LevelId.StartsWith("level.",StringComparison.Ordinal))throw new InvalidOperationException("Arena 身份不符合本轮已复制的关卡。");
                    var config=ScriptableObject.CreateInstance<TopdownLevelDefinition>();config.Configure(identity.LevelId,"俯视障碍竞技场",scenePath,Resources.Load<PlayerScheme>("Config/Player/PlayerScheme"));
                    AssetDatabase.CreateAsset(config,"Assets/GameResource/Resources/Config/Level/TopdownArena.asset");
                    if(!EditorBuildSettings.scenes.Any(s=>s.path==scenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(scenePath,true)}).ToArray();
                }
                roots.SelectMany(r=>r.GetComponentsInChildren<PlayerController>(true)).Single().transform.position=new Vector2(-6,-4);
                roots.SelectMany(r=>r.GetComponentsInChildren<TopdownInteraction>(true)).Single().transform.position=new Vector2(-5,-4);
                foreach(var actor in roots.SelectMany(r=>r.GetComponentsInChildren<TopdownCombatActor>(true)))actor.transform.position=actor.Chase?new Vector2(5,4):new Vector2(0,-2);
                var wall=new GameObject("Arena central obstacle");wall.transform.position=new Vector2(-3,0);wall.transform.localScale=new Vector3(5,.7f,1);
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Player/Topdown/Square.png");
                wall.AddComponent<SpriteRenderer>().sprite=sprite;wall.GetComponent<SpriteRenderer>().color=new Color(.25f,.35f,.4f);wall.GetComponent<SpriteRenderer>().sortingOrder=2;
                wall.AddComponent<BoxCollider2D>();EditorSceneManager.SaveScene(scene);
                var obstacle=new GameObject("SolidObstacle");
                try
                {obstacle.AddComponent<BoxCollider2D>();var renderer=obstacle.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=new Color(.25f,.35f,.4f);renderer.sortingOrder=2;PrefabUtility.SaveAsPrefabAsset(obstacle,"Assets/GameResource/Player/Topdown/SolidObstacle.prefab");}
                finally{UnityEngine.Object.DestroyImmediate(obstacle);}
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            Material("checkpoint","持久检查点","Assets/GameResource/Player/Topdown/Checkpoint.prefab",false);
            Material("chaser","追击敌人","Assets/GameResource/Player/Topdown/Chaser.prefab",true);
            Material("target","训练沙袋","Assets/GameResource/Player/Topdown/TrainingTarget.prefab",true);
            Material("wall","实体障碍","Assets/GameResource/Player/Topdown/SolidObstacle.prefab",true);
            AssetDatabase.SaveAssets();
            BindConfigurations();
        }
        public static void BindConfigurations()
        {
            TopdownLevelAdapter.RequireWrite();var setup=EditorSceneManager.GetSceneManagerSetup();
            var paths=TopdownLevelAdapter.Instance.Discover().Select(l=>AssetDatabase.GetAssetPath(l.configuration)).ToArray();
            try
            {
                foreach(var path in paths)
                {
                    var definition=AssetDatabase.LoadAssetAtPath<TopdownLevelDefinition>(path);var scenePath=definition.ScenePath;
                    var scene=EditorSceneManager.OpenScene(scenePath);definition=AssetDatabase.LoadAssetAtPath<TopdownLevelDefinition>(path);
                    var identity=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TopdownLevelIdentity>(true)).Single();
                    if(identity.LevelId!=definition.Id||identity.ScenePath!=definition.ScenePath)throw new InvalidOperationException("已有场景身份与定义不一致，未覆盖。");
                    using(var data=new SerializedObject(identity)){data.FindProperty("_configuration").objectReferenceValue=definition;data.ApplyModifiedPropertiesWithoutUndo();}EditorSceneManager.SaveScene(scene);
                }
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        #endregion
    }
}
