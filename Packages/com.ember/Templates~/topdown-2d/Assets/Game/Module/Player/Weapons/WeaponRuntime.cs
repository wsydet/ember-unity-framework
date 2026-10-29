using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player.Weapons
{
    /// <summary>由装备者适配；武器不依赖玩家状态类型，NPC也可提供此边界。</summary>
    public interface IWeaponActor
    {
        Vector2 AimDirection {get;}
        bool CanOperateWeapon {get;}
        IDisposable AcquireWeaponControl(object owner,bool lockFacing);
    }
    public sealed class WeaponContext:IDisposable
    {
        #region 内部参数
        private readonly IWeaponActor _actor;
        private IDisposable _control;
        private bool _emitted;
        public WeaponSpec Spec {get;}
        public EntityPowerHost Powers {get;}
        public bool FirePending {get;internal set;}
        public bool CanFire=>_actor.CanOperateWeapon&&Powers.Skills.Query("fire")==EntityUseStatus.Available;
        public Vector2 Direction {get;private set;}
        public ulong AttackInstance {get;private set;}
        public bool Emission {get;private set;}
        public HashSet<Entity2D> HitLedger {get;}=new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal WeaponContext(WeaponSpec spec,IWeaponActor actor,EntityPowerSnapshot restore)
        {Spec=spec;_actor=actor;Powers=restore==null?EntityPowerHost.Create(spec.Powers):EntityPowerHost.Restore(spec.Powers,restore);}
        public void BeginAttack()
        {
            if(!CanFire||!Powers.Skills.TryGet("fire",out var skill)||skill.TryCommit(skill.CreateAttempt())!=EntityUseStatus.Available)
                throw new InvalidOperationException("武器 fire 资源提交失败。");
            Direction=_actor.AimDirection.normalized;AttackInstance++;HitLedger.Clear();_emitted=false;
            if(Spec.LockMovement)_control=_actor.AcquireWeaponControl(this,Spec.LockFacing);
        }
        public void Emit(){if(Spec.Mode==WeaponHitMode.Melee||!_emitted){Emission=true;_emitted=true;}}
        internal void BeginStep(){Emission=false;}
        public void EndAttack(){_control?.Dispose();_control=null;FirePending=false;}
        public void Dispose(){EndAttack();Powers.Dispose();HitLedger.Clear();Emission=false;}
        #endregion
    }
    /// <summary>每把已装配武器的独立会话。只有 PlayerModule 的驱动链调用 Tick/Step。</summary>
    public sealed class WeaponRuntime:IDisposable
    {
        #region 内部参数
        private readonly EmberFsmObservationOrigin _origin;
        private bool _started,_paused,_disposed;
        private float _tracer;
        public WeaponSpec Spec {get;}
        public WeaponContext Context {get;}
        private EmberFsm<WeaponContext> Fsm {get;set;}
        public string CurrentState=>Fsm.CurrentLeafId;
        public bool IsReady=>Fsm.CurrentLeafId=="ready";
        public float TracerDistance {get;internal set;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal WeaponRuntime(WeaponSpec spec,IWeaponActor actor,EntityPowerSnapshot restore)
        {
            Spec=spec;Context=new WeaponContext(spec,actor,restore);
            try{Fsm=new EmberFsm<WeaponContext>(spec.Graph,Context);_origin=EmberFsmObservationOrigin.Capture(spec.SourceGraph,spec.Graph);}
            catch{Context.Dispose();throw;}
        }
        internal void Start(){if(Fsm.Start()!=EmberFsmResult.Changed)throw new InvalidOperationException("武器状态机启动失败。");_started=true;}
        internal void Tick(float dt,bool equipped)
        {if(_disposed||_paused)return;Context.Powers.Tick(dt);_tracer=Mathf.Max(0,_tracer-dt);if(equipped){Fsm.Update(dt);CheckFsm();}}
        internal void Step(float dt,bool fire,bool allowed)
        {
            Context.BeginStep();if(_disposed||_paused)return;
            if(!allowed){Interrupt();return;}
            Context.FirePending=fire;Fsm.FixedUpdate(dt);Context.FirePending=false;
            CheckFsm();
            if(Context.Emission&&Spec.Mode==WeaponHitMode.Ray){_tracer=.07f;TracerDistance=Spec.Range;}
        }
        internal void Interrupt()
        {
            Context.BeginStep();_tracer=0;
            if(!_started||IsReady){Context.EndAttack();return;}
            Fsm.Dispose();Context.EndAttack();Fsm=new EmberFsm<WeaponContext>(Spec.Graph,Context);Start();if(_paused)Fsm.Pause();
        }
        internal void SetPaused(bool paused){if(_paused==paused)return;_paused=paused;Context.FirePending=false;if(paused)Fsm.Pause();else Fsm.Resume();}
        public EmberFsmObservation CaptureObservation()=>EmberFsmObservation.Capture(Fsm,_origin);
        private void CheckFsm(){if(Fsm.Status==EmberFsmStatus.Faulted)throw new InvalidOperationException("武器状态机故障："+Fsm.LastFault);}
        internal void Present(SpriteRenderer renderer,Vector2 aim)
        {
            if(renderer==null)return;renderer.enabled=true;var direction=IsReady?aim:Context.Direction;
            var firing=Spec.Mode==WeaponHitMode.Ray&&_tracer>0;
            var size=firing?new Vector2(TracerDistance,.045f):Spec.Mode==WeaponHitMode.Melee&&Fsm.CurrentLeafId=="active"?Spec.Size:new Vector2(.5f,.12f);
            var offset=firing?new Vector2(TracerDistance*.5f,0):Spec.Mode==WeaponHitMode.Melee&&Fsm.CurrentLeafId=="active"?Spec.Offset:new Vector2(.65f,0);
            renderer.transform.localPosition=EntityPlaneFrame.Transform(offset,direction);renderer.transform.localRotation=Quaternion.Euler(0,0,EntityPlaneFrame.Angle(direction));
            renderer.transform.localScale=new Vector3(size.x,size.y,1);renderer.color=Spec.Mode==WeaponHitMode.Ray?new Color(1,.85f,.25f,.85f):Spec.Color;
        }
        public void Dispose(){if(_disposed)return;_disposed=true;try{Fsm.Dispose();}finally{Context.Dispose();}}
        #endregion
    }
    public sealed class WeaponLoadout:IDisposable
    {
        #region 内部参数
        private readonly List<WeaponRuntime> _weapons=new();
        private int _selected;
        public IReadOnlyList<WeaponRuntime> Weapons=>_weapons;
        public WeaponRuntime Current=>_weapons.Count==0?null:_weapons[_selected];
        public bool Stable {get{foreach(var weapon in _weapons)if(!weapon.IsReady)return false;return true;}}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal WeaponLoadout(IReadOnlyList<WeaponSpec> specs,IWeaponActor actor,WeaponLoadoutProgress restore)
        {
            if(restore!=null&&restore.Count!=specs.Count)throw new InvalidOperationException("存档武器装配集合不兼容。");
            try
            {
                for(var i=0;i<specs.Count;i++)
                {
                    var record=restore?.Get(i);if(record.HasValue&&record.Value.Id!=specs[i].Id)throw new InvalidOperationException("存档武器身份不兼容。");
                    _weapons.Add(new WeaponRuntime(specs[i],actor,record?.Powers));if(restore?.EquippedId==specs[i].Id)_selected=i;
                }
            }
            catch{Dispose();throw;}
        }
        internal void Start(){foreach(var weapon in _weapons)weapon.Start();}
        internal void Tick(float dt){foreach(var weapon in _weapons)weapon.Tick(dt,ReferenceEquals(weapon,Current));}
        internal void Step(float dt,bool fire,bool allowed)=>Current?.Step(dt,fire,allowed);
        internal void Interrupt()=>Current?.Interrupt();
        internal void SetPaused(bool paused){foreach(var weapon in _weapons)weapon.SetPaused(paused);}
        internal bool Equip(string id)
        {for(var i=0;i<_weapons.Count;i++)if(_weapons[i].Spec.Id==id){if(i==_selected)return true;Current?.Interrupt();_selected=i;return true;}return false;}
        internal void Next(){if(_weapons.Count>1)Equip(_weapons[(_selected+1)%_weapons.Count].Spec.Id);}
        internal WeaponLoadoutProgress Capture()
        {if(!Stable)throw new InvalidOperationException("武器攻击尚未结束，请等待稳定状态再保存。");var records=new WeaponProgress[_weapons.Count];for(var i=0;i<records.Length;i++)records[i]=new WeaponProgress(_weapons[i].Spec.Id,_weapons[i].Context.Powers.Capture());return new WeaponLoadoutProgress(Current?.Spec.Id??"",records);}
        public void Dispose(){foreach(var weapon in _weapons)weapon.Dispose();_weapons.Clear();}
        #endregion
    }
    public readonly struct WeaponProgress
    {
        public string Id {get;}
        public EntityPowerSnapshot Powers {get;}
        public WeaponProgress(string id,EntityPowerSnapshot powers){TopdownSnapshot.ValidateId(id);Id=id;Powers=powers??throw new ArgumentNullException(nameof(powers));}
    }
    public sealed class WeaponLoadoutProgress
    {
        #region 内部参数
        private readonly WeaponProgress[] _records;
        public string EquippedId {get;}
        public int Count=>_records.Length;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public WeaponLoadoutProgress(string equippedId,WeaponProgress[] records)
        {
            if(records==null||records.Length>16)throw new ArgumentException("武器进度集合无效。");
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var record in records)
            {TopdownSnapshot.ValidateId(record.Id);if(record.Powers==null||record.Powers.SkillCount>256||record.Powers.AbilityCount>256||!ids.Add(record.Id))throw new ArgumentException("武器进度重复或不完整。");
                for(var i=0;i<record.Powers.SkillCount;i++)TopdownSnapshot.ValidateId(record.Powers.GetSkill(i).Id);for(var i=0;i<record.Powers.AbilityCount;i++)TopdownSnapshot.ValidateId(record.Powers.GetAbility(i).Id);}
            if(records.Length==0?equippedId!="":!ids.Contains(equippedId))throw new ArgumentException("当前武器不属于进度集合。");
            EquippedId=equippedId;_records=(WeaponProgress[])records.Clone();
        }
        public WeaponProgress Get(int index)=>_records[index];
        #endregion
    }
}
