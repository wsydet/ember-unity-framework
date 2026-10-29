using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player.Weapons
{
    public enum WeaponHitMode { Melee, Ray }

    /// <summary>武器拥有图、攻击参数与资源；不引用 PlayerContext，也不推进角色状态机。</summary>
    [CreateAssetMenu(menuName="Game/Player/Weapon",fileName="Weapon")]
    public sealed class WeaponDefinition:EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _id="weapon.sword",_displayName="训练剑";
        [SerializeField] private EmberFsmGraph _graph;
        [SerializeField] private EntityPowerConfig _powers;
        [SerializeField] private WeaponHitMode _mode;
        [SerializeField,Min(.001f)] private float _windup=.06f,_active=.08f,_recovery=.21f;
        [SerializeField] private bool _lockMovement=true,_lockFacing=true;
        [SerializeField] private Vector2 _offset=new(.9f,0),_size=new(1.3f,1);
        [SerializeField,Min(.01f)] private float _range=9;
        [SerializeField,Min(0)] private float _damage=2,_stun=.18f,_braking=30;
        [SerializeField] private Vector2 _knockback=new(4,0);
        [SerializeField] private Color _color=new(.6f,1,1,.6f);
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id=>_id;
        public string DisplayName=>_displayName;
        public EmberFsmGraph Graph=>_graph;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(string id,string displayName,EmberFsmGraph graph,EntityPowerConfig powers,
            WeaponHitMode mode=WeaponHitMode.Melee,bool lockMovement=true,bool lockFacing=true,
            float windup=.06f,float active=.08f,float recovery=.21f)
        {_id=id;_displayName=displayName;_graph=graph;_powers=powers;_mode=mode;_lockMovement=lockMovement;_lockFacing=lockFacing;_windup=windup;_active=active;_recovery=recovery;}
        public WeaponSpec Capture()
        {
            TopdownSnapshot.ValidateId(_id);
            if(string.IsNullOrWhiteSpace(_displayName)||_graph==null||!_graph.Inspect(typeof(WeaponContext)).IsValid||
                _graph.EntryId!="ready"||!Enum.IsDefined(typeof(WeaponHitMode),_mode)||_lockFacing&&!_lockMovement)
                throw new InvalidOperationException("武器身份、独立图入口或运动所有权配置无效。");
            foreach(var value in new[]{_windup,_active,_recovery,_offset.x,_offset.y,_size.x,_size.y,_range,_damage,_stun,_braking,_knockback.x,_knockback.y,_color.r,_color.g,_color.b,_color.a})
                if(!float.IsFinite(value))throw new InvalidOperationException("武器参数必须有限。");
            if(_windup<=0||_active<=0||_recovery<=0||_size.x<=0||_size.y<=0||_range<=0||_damage<0||_stun<0||_braking<0)
                throw new InvalidOperationException("武器时序、形状或伤害无效。");
            return new WeaponSpec(this,_powers,_mode,_windup,_active,_recovery,_lockMovement,_lockFacing,_offset,_size,_range,_damage,_stun,_braking,_knockback,_color);
        }
        #endregion
    }

    public sealed class WeaponSpec:IDisposable
    {
        #region 内部参数
        private readonly List<UnityEngine.Object> _owned=new();
        public string Id {get;}
        public string DisplayName {get;}
        public EmberFsmGraph Graph {get;}
        public EmberFsmGraph SourceGraph {get;}
        public EntityPowerConfig Powers {get;}
        public WeaponHitMode Mode {get;}
        public float Windup {get;}
        public float Active {get;}
        public float Recovery {get;}
        public bool LockMovement {get;}
        public bool LockFacing {get;}
        public Vector2 Offset {get;}
        public Vector2 Size {get;}
        public float Range {get;}
        public float Damage {get;}
        public float Stun {get;}
        public float Braking {get;}
        public Vector2 Knockback {get;}
        public Color Color {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal WeaponSpec(WeaponDefinition source,EntityPowerConfig powers,WeaponHitMode mode,float windup,float active,float recovery,
            bool lockMovement,bool lockFacing,Vector2 offset,Vector2 size,float range,float damage,float stun,float braking,Vector2 knockback,Color color)
        {
            Id=source.Id;DisplayName=source.DisplayName;SourceGraph=source.Graph;Mode=mode;Windup=windup;Active=active;Recovery=recovery;
            LockMovement=lockMovement;LockFacing=lockFacing;Offset=offset;Size=size;Range=range;Damage=damage;Stun=stun;Braking=braking;Knockback=knockback;Color=color;
            try
            {
                Powers=PlayerPowersDefinition.Freeze(powers,_owned);
                using(var host=Powers==null?EntityPowerHost.CreateEmpty():EntityPowerHost.Create(Powers))
                    if(!host.Skills.TryGet("fire",out _))throw new InvalidOperationException("武器缺少独立 fire 资源。");
                Graph=ScriptableObject.CreateInstance<EmberFsmGraph>();Graph.hideFlags=HideFlags.HideAndDontSave;_owned.Add(Graph);
                var nodes=new List<EmberFsmNode>();
                foreach(var node in source.Graph.Nodes)
                {
                    var definition=UnityEngine.Object.Instantiate(node.Definition);definition.hideFlags=HideFlags.HideAndDontSave;_owned.Add(definition);
                    var bindings=new List<EmberFsmTargetBinding>();foreach(var binding in node.Bindings)bindings.Add(new EmberFsmTargetBinding(binding.Slot,binding.TargetId));
                    nodes.Add(new EmberFsmNode(node.Id,node.DisplayName,definition,node.ParentId,node.DefaultChildId,bindings.ToArray()));
                }
                Graph.Configure(source.Graph.EntryId,nodes.ToArray());
            }
            catch{Dispose();throw;}
        }
        public EntityHit Hit(Vector2 direction)=>new("physical",Damage,EntityPlaneFrame.Transform(Knockback,direction),Braking,Stun);
        public void Dispose(){foreach(var asset in _owned)if(asset!=null)UnityEngine.Object.DestroyImmediate(asset);_owned.Clear();}
        #endregion
    }
}
