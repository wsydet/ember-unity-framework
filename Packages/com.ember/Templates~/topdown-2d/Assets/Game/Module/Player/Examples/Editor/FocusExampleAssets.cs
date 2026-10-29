using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Editor;
using Ember.Core.Fsm;
using Game.Entity;
using Game.Player.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Examples.Editor
{
    public static class FocusExampleAssets
    {
        #region 外部方法
        public static void Create()
        {
            if(!EmberProjectSetup.IsTemplateActive("topdown-2d")||EditorApplication.isPlayingOrWillChangePlaymode||
                ((EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(PlayerModule),typeof(EmberModuleAttribute)))?.Enabled!=true)
                throw new InvalidOperationException("扩展样例需要活动俯视模板与启用模块。");
            const string folder="Assets/GameResource/Resources/Config/Player/FocusExample";
            var scheme=PlayerSchemeWorkflow.Copy(Resources.Load<PlayerScheme>("Config/Player/PlayerScheme"),folder);
            var locomotion=ScriptableObject.CreateInstance<ExampleLocomotionDefinition>();AssetDatabase.CreateAsset(locomotion,folder+"/ExampleLocomotion.asset");
            var focus=ScriptableObject.CreateInstance<FocusStateDefinition>();AssetDatabase.CreateAsset(focus,folder+"/Focus.asset");
            var nodes=new List<EmberFsmNode>();
            foreach(var n in scheme.Graph.Nodes)
            {
                var bindings=n.Bindings.Select(b=>new EmberFsmTargetBinding(b.Slot,b.TargetId)).ToList();if(n.Id=="locomotion")bindings.Add(new EmberFsmTargetBinding("focus","focus"));
                nodes.Add(new EmberFsmNode(n.Id,n.DisplayName,n.Id=="locomotion"?locomotion:n.Definition,n.ParentId,n.DefaultChildId,bindings.ToArray()));
            }
            nodes.Add(new EmberFsmNode("focus","专注示例",focus,"action",null,new EmberFsmTargetBinding("locomotion","locomotion")));scheme.Graph.Configure("alive",nodes.ToArray());EditorUtility.SetDirty(scheme.Graph);
            using var schemeData=new SerializedObject(scheme);var powers=(PlayerPowersDefinition)schemeData.FindProperty("_powers").objectReferenceValue;
            var skill=ScriptableObject.CreateInstance<EntitySkillDefinition>();using(var data=new SerializedObject(skill))
            {data.FindProperty("_id").stringValue="focus";data.FindProperty("_defaultOpen").boolValue=true;data.FindProperty("_cooldownSeconds").floatValue=1;data.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.CreateAsset(skill,folder+"/focus-skill.asset");powers.Configure(powers.AbilitySlots.ToArray(),powers.SkillSlots.Concat(new[]{new EntitySkillSlot(skill)}).ToArray());EditorUtility.SetDirty(powers);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Player/Topdown/Square.png");scheme.ConfigureAnimation(new[]{"idle","move","dash","hit","dead","focus"}.Select(id=>new PlayerAnimationMapping(id,new[]{sprite})).ToArray(),false);
            using(var capture=scheme.Capture()){}EditorUtility.SetDirty(scheme);
            Complete();
        }
        /// <summary>仅续接本轮已创建的独立 Focus 方案；检查通过后补充输入并登记到 Arena。</summary>
        public static void Complete()
        {
            if(!EmberProjectSetup.IsTemplateActive("topdown-2d")||EditorApplication.isPlayingOrWillChangePlaymode||
                ((EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(PlayerModule),typeof(EmberModuleAttribute)))?.Enabled!=true)
                throw new InvalidOperationException("只能在俯视模板启用模块的空闲编辑状态续接。");
            var scheme=AssetDatabase.LoadAssetAtPath<PlayerScheme>("Assets/GameResource/Resources/Config/Player/FocusExample/PlayerScheme.asset");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Player/Topdown/Square.png");
            scheme.ConfigureAnimation(new[]{"idle","move","dash","hit","dead","focus"}.Select(id=>new PlayerAnimationMapping(id,new[]{sprite})).ToArray(),false);
            EditorUtility.SetDirty(scheme);using(var check=scheme.Capture()){}AssetDatabase.SaveAssets();
            const string inputPath="Assets/GameResource/Resources/Config/GameInput/Topdown.inputactions";
            var actions=InputActionAsset.FromJson(File.ReadAllText(inputPath));
            try
            {
                var existing=actions.FindAction("Gameplay/Focus");
                if(existing==null){actions.FindActionMap("Gameplay").AddAction("Focus",InputActionType.Button,"<Keyboard>/f").AddBinding("<Gamepad>/buttonNorth");File.WriteAllText(inputPath,actions.ToJson());}
                else if(existing.type!=InputActionType.Button||!existing.bindings.Any(b=>b.path=="<Keyboard>/f")||!existing.bindings.Any(b=>b.path=="<Gamepad>/buttonNorth"))throw new InvalidOperationException("已有 Focus 输入与本轮样例不一致，未覆盖。");
            }
            finally{UnityEngine.Object.DestroyImmediate(actions);}
            AssetDatabase.ImportAsset(inputPath,ImportAssetOptions.ForceUpdate);
            var arena=AssetDatabase.LoadAssetAtPath<Game.LevelAuthoring.TopdownLevelDefinition>("Assets/GameResource/Resources/Config/Level/TopdownArena.asset");
            arena.Configure(arena.Id,arena.DisplayName,arena.ScenePath,scheme);EditorUtility.SetDirty(arena);AssetDatabase.SaveAssets();
        }
        #endregion
    }
}
