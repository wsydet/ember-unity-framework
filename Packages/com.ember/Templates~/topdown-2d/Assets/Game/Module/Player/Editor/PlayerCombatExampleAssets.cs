using System;
using System.IO;
using Game.Entity;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>将已完成的 T2 起点装配为可选战斗示例。只操作活动 Assets。</summary>
    public static class PlayerCombatExampleAssets
    {
        #region 内部方法
        private const string CONFIG="Assets/GameResource/Resources/Config/Player/";
        private const string ART="Assets/GameResource/Player/Topdown/";
        private static EntitySkillDefinition Skill(string id,float cooldown)
        {
            var result=ScriptableObject.CreateInstance<EntitySkillDefinition>();
            using(var data=new SerializedObject(result))
            {data.FindProperty("_id").stringValue=id;data.FindProperty("_defaultOpen").boolValue=true;data.FindProperty("_cooldownSeconds").floatValue=cooldown;data.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.CreateAsset(result,CONFIG+id+".asset");return result;
        }
        private static GameObject ActorPrefab(string name,string id,bool chase,Sprite sprite)
        {
            var path=ART+name+".prefab";
            if(File.Exists(path))throw new InvalidOperationException("禁止覆盖已有战斗 Prefab："+path);
            var root=new GameObject(name){layer=2};
            try
            {
                root.AddComponent<Entity2D>();var body=root.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
                body.constraints=RigidbodyConstraints2D.FreezeRotation;body.simulated=false;root.AddComponent<BoxCollider2D>().size=new Vector2(.8f,.8f);
                var visual=new GameObject("Body");visual.transform.SetParent(root.transform,false);visual.transform.localScale=new Vector3(.8f,.8f,1);
                var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=chase?new Color(.95f,.3f,.25f):new Color(.6f,.4f,.95f);renderer.sortingOrder=9;
                var actor=root.AddComponent<TopdownCombatActor>();
                using(var data=new SerializedObject(actor))
                {data.FindProperty("_stableId").stringValue=id;data.FindProperty("_chase").boolValue=chase;data.FindProperty("_health").floatValue=chase?4:12;data.ApplyModifiedPropertiesWithoutUndo();}
                return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public static void Create()
        {
            PlayerEditorAccess.RequireWrite();
            if(File.Exists(CONFIG+"PlayerPowers.asset"))throw new InvalidOperationException("战斗示例已装配，禁止重复覆盖。");
            var source=AssetDatabase.LoadAssetAtPath<PlayerScheme>(CONFIG+"PlayerScheme.asset");
            if(source==null)throw new InvalidOperationException("缺少 T2 方案。");
            var snapshot=EditorSceneManager.GetSceneManagerSetup();
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try
            {
                var definitions=new PlayerStateDefinition[9];
                foreach(PlayerStateKind kind in Enum.GetValues(typeof(PlayerStateKind)))
                {
                    var path=CONFIG+"States/"+kind+".asset";
                    var definition=AssetDatabase.LoadAssetAtPath<PlayerStateDefinition>(path);
                    if(definition==null){definition=ScriptableObject.CreateInstance<PlayerStateDefinition>();definition.Configure(kind);AssetDatabase.CreateAsset(definition,path);}
                    definitions[(int)kind]=definition;
                }
                var graph=source.Graph;
                graph.Configure("alive",
                    new EmberFsmNode("alive","存活",definitions[0],null,"locomotion",new EmberFsmTargetBinding("dead","dead"),new EmberFsmTargetBinding("hit","hit")),
                    new EmberFsmNode("locomotion","移动域",definitions[1],"alive","idle",new EmberFsmTargetBinding("dash","dash")),
                    new EmberFsmNode("idle","待机",definitions[2],"locomotion",null,new EmberFsmTargetBinding("move","move")),
                    new EmberFsmNode("move","移动",definitions[3],"locomotion",null,new EmberFsmTargetBinding("idle","idle")),
                    new EmberFsmNode("action","动作域",definitions[4],"alive","dash"),
                    new EmberFsmNode("dash","冲刺",definitions[6],"action",null,new EmberFsmTargetBinding("locomotion","locomotion")),
                    new EmberFsmNode("hit","受击",definitions[7],"alive",null,new EmberFsmTargetBinding("locomotion","locomotion")),
                    new EmberFsmNode("dead","死亡",definitions[8]));
                if(!graph.Inspect(typeof(PlayerContext)).IsValid)throw new InvalidOperationException("战斗图校验失败。");EditorUtility.SetDirty(graph);
                var powers=ScriptableObject.CreateInstance<PlayerPowersDefinition>();
                powers.Configure(Array.Empty<EntityAbilitySlot>(),new[]{new EntitySkillSlot(Skill("dash",.65f))});
                AssetDatabase.CreateAsset(powers,CONFIG+"PlayerPowers.asset");source.ConfigureCombat(true,powers);
                source.ConfigureWeapons(WeaponExampleAssets.CreateDefaults());
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ART+"Square.png");
                var maps=new PlayerAnimationMapping[5];var ids=new[]{"idle","move","dash","hit","dead"};
                for(var i=0;i<ids.Length;i++)maps[i]=new PlayerAnimationMapping(ids[i],new[]{sprite});source.ConfigureAnimation(maps,false);EditorUtility.SetDirty(source);
                var playerPath=ART+"Player.prefab";var prefab=PrefabUtility.LoadPrefabContents(playerPath);
                try
                {
                    if(prefab.GetComponent<Entity2D>()==null)prefab.AddComponent<Entity2D>();
                    var melee=new GameObject("Weapon");melee.transform.SetParent(prefab.transform,false);
                    var renderer=melee.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=12;renderer.color=new Color(.6f,1,1,.4f);renderer.enabled=false;
                    PrefabUtility.SaveAsPrefabAsset(prefab,playerPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                var enemy=ActorPrefab("Chaser","enemy.training",true,sprite);var dummy=ActorPrefab("TrainingTarget","target.training",false,sprite);
                var scene=EditorSceneManager.OpenScene("Assets/Game/Scenes/GameplayScene.unity");
                var enemyInstance=(GameObject)PrefabUtility.InstantiatePrefab(enemy,scene);enemyInstance.transform.position=new Vector3(-5,2,0);
                var targetInstance=(GameObject)PrefabUtility.InstantiatePrefab(dummy,scene);targetInstance.transform.position=new Vector3(2,-2,0);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();using(var captured=source.Capture()){}
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(snapshot);}
        }
        #endregion
    }
}
