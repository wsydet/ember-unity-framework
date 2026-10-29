using System;
using System.Collections.Generic;
using Ember.LevelAuthoring.Editor;
using Game.Entity;
using Game.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.LevelAuthoring.Editor
{
    internal sealed class TopdownObjectAdapter:IEmberLevelObjectAdapter
    {
        #region 内部参数
        internal static readonly TopdownObjectAdapter Instance=new();
        public string Id=>"topdown.object";
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Register()
        {EmberLevelAdapters.Register(Instance,"topdown-2d",()=>TopdownLevelAdapter.Available);EmberLevelAdapters.RegisterObjectResolver("topdown.objects",go=>TopdownLevelAdapter.Available&&(go.GetComponent<TopdownInteraction>()!=null||go.GetComponent<TopdownCombatActor>()!=null)?Instance:null);}
        public void ValidatePlacement(EmberLevelMaterial material,Scene scene)
        {TopdownLevelAdapter.RequireWrite();if(!scene.IsValid()||!scene.isLoaded)throw new InvalidOperationException("目标关卡尚未打开。");}
        public void OnCreated(GameObject instance,bool duplicate)
        {
            TopdownLevelAdapter.RequireWrite();
            foreach(var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if(component is TopdownInteraction||component is TopdownCombatActor)
                {using var data=new SerializedObject(component);data.FindProperty("_stableId").stringValue="object."+Guid.NewGuid().ToString("N");data.ApplyModifiedProperties();}
        }
        public void BeforeDelete(GameObject instance)=>TopdownLevelAdapter.RequireWrite();
        public IEnumerable<EmberLevelDiagnostic> Validate(GameObject instance)
        {
            if(!TopdownLevelAdapter.Available)yield break;
            var actor=instance.GetComponent<TopdownCombatActor>();var interaction=instance.GetComponent<TopdownInteraction>();
            if(actor!=null)
            {
                if(string.IsNullOrWhiteSpace(actor.StableId)||actor.Health<=0||!float.IsFinite(actor.Health))yield return new EmberLevelDiagnostic("TD.actor","角色需要稳定身份和正生命上限。",EmberLevelSeverity.Error,actor);
                var body=actor.GetComponent<Rigidbody2D>();if(body==null||body.gravityScale!=0||body.bodyType!=RigidbodyType2D.Kinematic)yield return new EmberLevelDiagnostic("TD.actor.body","角色需要零重力 Kinematic 身体。",EmberLevelSeverity.Error,actor);
            }
            if(interaction!=null&&(string.IsNullOrWhiteSpace(interaction.StableId)||!float.IsFinite(interaction.Radius)||interaction.Radius<=0))
                yield return new EmberLevelDiagnostic("TD.interaction","交互需要稳定身份和正有限半径。",EmberLevelSeverity.Error,interaction);
        }
        public void DrawScene(GameObject instance)
        {
            if(!TopdownLevelAdapter.Available)return;
            var interaction=instance.GetComponent<TopdownInteraction>();var actor=instance.GetComponent<TopdownCombatActor>();
            if(interaction!=null){Handles.color=Color.cyan;Handles.DrawWireDisc(instance.transform.position,Vector3.forward,interaction.Radius);}
            if(actor!=null){Handles.color=new Color(1,.55f,.15f);Handles.DrawWireDisc(instance.transform.position,Vector3.forward,actor.DetectionRange);}
        }
        #endregion
    }
}
