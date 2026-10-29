using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using Game.Player.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Editor
{
    /// <summary>活动 Assets 中的武器示例与本轮未发布起点迁移；不操作模板快照。</summary>
    public static class WeaponExampleAssets
    {
        #region 内部参数
        private const string ROOT="Assets/GameResource/Resources/Config/Weapon/";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static WeaponDefinition Create(string folder,string id,string name,WeaponHitMode mode)
        {
            var path=ROOT+folder;
            if(Directory.Exists(path))throw new InvalidOperationException("禁止覆盖已有武器目录："+path);
            Directory.CreateDirectory(path);AssetDatabase.Refresh();
            var skill=ScriptableObject.CreateInstance<EntitySkillDefinition>();
            using(var data=new SerializedObject(skill))
            {data.FindProperty("_id").stringValue="fire";data.FindProperty("_defaultOpen").boolValue=true;data.FindProperty("_cooldownSeconds").floatValue=mode==WeaponHitMode.Melee?.3f:.24f;data.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.CreateAsset(skill,path+"/Fire.asset");
            var powers=ScriptableObject.CreateInstance<PlayerPowersDefinition>();powers.Configure(Array.Empty<EntityAbilitySlot>(),new[]{new EntitySkillSlot(skill)});AssetDatabase.CreateAsset(powers,path+"/Powers.asset");
            var nodes=new List<EmberFsmNode>();var ids=new[]{"ready","windup","active","recovery"};
            for(var i=0;i<4;i++)
            {
                var state=ScriptableObject.CreateInstance<WeaponStateDefinition>();state.Configure((WeaponStateKind)i);AssetDatabase.CreateAsset(state,path+"/"+ids[i]+".asset");
                nodes.Add(new EmberFsmNode(ids[i],new[]{"就绪","前摇","命中","后摇"}[i],state,null,null,new EmberFsmTargetBinding(ids[(i+1)%4],ids[(i+1)%4])));
            }
            var graph=ScriptableObject.CreateInstance<EmberFsmGraph>();graph.Configure("ready",nodes.ToArray());AssetDatabase.CreateAsset(graph,path+"/Graph.asset");
            var weapon=ScriptableObject.CreateInstance<WeaponDefinition>();weapon.Configure(id,name,graph,powers,mode,mode==WeaponHitMode.Melee,mode==WeaponHitMode.Melee,
                mode==WeaponHitMode.Melee?.06f:.02f,mode==WeaponHitMode.Melee?.08f:.02f,mode==WeaponHitMode.Melee?.21f:.18f);
            AssetDatabase.CreateAsset(weapon,path+"/Weapon.asset");using(var check=weapon.Capture()){}return weapon;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static WeaponDefinition[] CreateDefaults()
        {
            PlayerEditorAccess.RequireWrite();
            var sword=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(ROOT+"Sword/Weapon.asset");var pistol=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(ROOT+"Pistol/Weapon.asset");
            if(sword==null)sword=Create("Sword","weapon.training-sword","训练剑",WeaponHitMode.Melee);
            if(pistol==null)pistol=Create("Pistol","weapon.training-pistol","训练手枪",WeaponHitMode.Ray);
            return new[]{sword,pistol};
        }
        public static void UpgradeActiveExamples()
        {
            PlayerEditorAccess.RequireWrite();var weapons=CreateDefaults();
            foreach(var path in new[]{"Assets/GameResource/Resources/Config/Player/PlayerScheme.asset","Assets/GameResource/Resources/Config/Player/FocusExample/PlayerScheme.asset"})
            {
                var scheme=AssetDatabase.LoadAssetAtPath<PlayerScheme>(path);if(scheme==null)throw new InvalidOperationException("缺少待迁移示例："+path);
                var nodes=new List<EmberFsmNode>();
                foreach(var node in scheme.Graph.Nodes)
                {
                    if(node.Id=="attack")continue;
                    var bindings=node.Bindings.Where(b=>b.Slot!="attack").Select(b=>new EmberFsmTargetBinding(b.Slot,b.TargetId)).ToArray();
                    nodes.Add(new EmberFsmNode(node.Id,node.DisplayName,node.Definition,node.ParentId,node.Id=="action"?"dash":node.DefaultChildId,bindings));
                }
                scheme.Graph.Configure(scheme.Graph.EntryId,nodes.ToArray());EditorUtility.SetDirty(scheme.Graph);scheme.ConfigureWeapons(weapons);
                using(var data=new SerializedObject(scheme))
                {
                    var powers=(PlayerPowersDefinition)data.FindProperty("_powers").objectReferenceValue;
                    using(var powerData=new SerializedObject(powers))
                    {
                        var slots=powerData.FindProperty("_skills");for(var i=slots.arraySize-1;i>=0;i--)
                        {var definition=(EntitySkillDefinition)slots.GetArrayElementAtIndex(i).FindPropertyRelative("_definition").objectReferenceValue;if(definition.Id=="attack")slots.DeleteArrayElementAtIndex(i);}
                        powerData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(powers);
                    }
                }
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Player/Topdown/Square.png");
                scheme.ConfigureAnimation(scheme.Graph.Nodes.Where(n=>string.IsNullOrEmpty(n.DefaultChildId)).Select(n=>new PlayerAnimationMapping(n.Id,new[]{sprite})).ToArray(),false);
                EditorUtility.SetDirty(scheme);using(var check=scheme.Capture()){}
            }
            const string inputPath="Assets/GameResource/Resources/Config/GameInput/Topdown.inputactions";var input=InputActionAsset.FromJson(File.ReadAllText(inputPath));
            try
            {if(input.FindAction("Gameplay/NextWeapon")==null)input.FindActionMap("Gameplay").AddAction("NextWeapon",InputActionType.Button,"<Keyboard>/q").AddBinding("<Gamepad>/leftShoulder");File.WriteAllText(inputPath,input.ToJson());}
            finally{UnityEngine.Object.DestroyImmediate(input);}
            AssetDatabase.ImportAsset(inputPath,ImportAssetOptions.ForceUpdate);AssetDatabase.SaveAssets();
        }
        #endregion
    }
}
