using System;
using System.Collections.Generic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>本会话唯一战斗驱动。推进→收集→稳定结算；不允许场景组件自行 Tick。</summary>
    internal sealed class PlayerCombatScene:IDisposable
    {
        #region 内部参数
        private readonly PlayerContext _player;
        private readonly EntityHitBatch _batch=new();
        private readonly PlayerHitQuery _query;
        private readonly EntityHitBatch.Target _playerTarget;
        private readonly List<Actor> _actors=new();
        private bool _started,_disposed;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class Actor:IEntityReactionSink,IDisposable
        {
            internal readonly TopdownCombatActor Source;
            internal readonly string StableId;
            internal readonly Entity2D Entity;
            internal readonly Rigidbody2D Body;
            internal readonly EntityMotor2D Motor;
            internal readonly EntityHealth Health;
            internal readonly EntityHitBatch.Target Target;
            internal readonly EntityEffectContainer Effects=new();
            private EntityEffectBinding _binding;
            private readonly SpriteRenderer _sprite;
            private readonly Color _baseColor;
            private readonly float _speed,_range,_damage;
            private readonly bool _chase;
            private readonly Vector2? _restorePosition;
            private bool _started;
            private EntityHit? _pendingHit;
            private Vector2 _velocity;
            private float _stun,_elapsed,_reactionBraking;
            internal Vector2 Direction=Vector2.right,From,To;
            internal string Phase="idle";
            internal ulong Attack;
            internal bool Emission;
            internal Actor(TopdownCombatActor source,EntityHitBatch batch,PlayerSchemeSnapshot scheme,TopdownActorRecord? restore,IPlayerCombatRules rules)
            {
                Source=source;StableId=source.StableId;Entity=source.GetComponent<Entity2D>();Body=source.GetComponent<Rigidbody2D>();
                var shape=source.GetComponent<BoxCollider2D>();
                if(string.IsNullOrWhiteSpace(source.StableId)||!float.IsFinite(source.Health)||source.Health<=0||
                    !float.IsFinite(source.Speed)||source.Speed<0||!float.IsFinite(source.DetectionRange)||source.DetectionRange<0||
                    !float.IsFinite(source.Damage)||source.Damage<0||Body.bodyType!=RigidbodyType2D.Kinematic||Body.gravityScale!=0)
                    throw new InvalidOperationException("俯视战斗对象身份、数值或零重力身体无效："+source.name);
                _speed=source.Speed;_range=source.DetectionRange;_damage=source.Damage;_chase=source.Chase;
                if(restore.HasValue&&restore.Value.Id!=StableId)throw new InvalidOperationException("世界角色快照身份不匹配。");
                _restorePosition=restore?.Position;
                Motor=new EntityMotor2D(Body,shape,scheme.Obstacles,scheme.Skin);Health=new EntityHealth(source.Health,restore?.Health??source.Health);
                if(restore.HasValue&&!Motor.CanOccupy(restore.Value.Position))throw new InvalidOperationException("快照世界角色位置与实体墙重叠。");
                _sprite=source.GetComponentInChildren<SpriteRenderer>();_baseColor=source.InitialColor;
                try{Target=batch.Register(Entity,source.StableId,source.Team,new EntityHitReceiver(Health,Effects,this,
                    rule:rules==null?null:new PlayerBoundHitRule(rules,StableId,source.Team,Body),isValid:()=>_started&&Source!=null&&Source.isActiveAndEnabled));}
                catch{Effects.Dispose();throw;}
            }
            internal void Start()
            {
                var shape=Source.GetComponent<BoxCollider2D>();
                _binding=new EntityEffectBinding(Effects,new Collider2D[]{shape},_sprite==null?Array.Empty<SpriteRenderer>():new[]{_sprite});
                Body.linearVelocity=Vector2.zero;Body.simulated=Health.Current>0;
                if(_restorePosition.HasValue){Source.transform.position=_restorePosition.Value;Body.position=_restorePosition.Value;}
                _started=true;
            }
            public bool Request(in EntityHit hit){if(Health.Current<=0)return false;_pendingHit=hit;return true;}
            internal void Step(PlayerContext player,float dt)
            {
                Emission=false;From=Body.position;
                if(Source==null||!Source.isActiveAndEnabled){Target.Dispose();return;}
                if(Health.Current<=0){Phase="dead";_velocity=Vector2.zero;Body.linearVelocity=Vector2.zero;Body.simulated=false;Colorize(new Color(.22f,.25f,.3f));return;}
                var justHit=_pendingHit.HasValue;
                if(justHit)
                {var hit=_pendingHit.Value;_pendingHit=null;_velocity=hit.Velocity;_stun=hit.StunSeconds;_reactionBraking=hit.Braking;Phase="hit";_elapsed=0;}
                if(_stun>0||justHit)
                {_stun=Mathf.Max(0,_stun-dt);if(!justHit)_velocity=_stun<=0?Vector2.zero:Vector2.MoveTowards(_velocity,Vector2.zero,_reactionBraking*dt);if(_stun==0)Phase="idle";}
                else if(_chase&&!player.IsDead)
                {
                    var delta=player.Controller.Body.position-Body.position;var distance=delta.magnitude;
                    _elapsed+=dt;
                    if(Phase=="windup")
                    { _velocity=Vector2.zero;if(_elapsed>=.45f){Phase="strike";Emission=true;Attack++;_elapsed=0;} }
                    else if(Phase=="strike"){Phase="recovery";_velocity=Vector2.zero;_elapsed=0;}
                    else if(Phase=="recovery"){_velocity=Vector2.zero;if(_elapsed>=.65f){Phase="idle";_elapsed=0;}}
                    else if(distance<=1.3f)
                    {Direction=distance>.0001f?delta/distance:Direction;Phase="windup";_elapsed=0;_velocity=Vector2.zero;}
                    else if(distance<=_range){Phase="chase";Direction=delta/distance;_velocity=Direction*_speed;}
                    else {Phase="idle";_velocity=Vector2.zero;}
                }
                else {_velocity=Vector2.zero;if(player.IsDead)Phase="idle";}
                _velocity=Motor.Step(_velocity,dt);To=Motor.Destination;
                Colorize(Phase=="windup"?new Color(1,.6f,.1f):Phase=="strike"?Color.white:_baseColor);
            }
            private void Colorize(Color color)
            {if(_sprite==null)return;color.a=_sprite.color.a;_sprite.color=color;}
            internal EntityHit Hit()=>new("physical",_damage,Direction*4,30,.2f);
            public void Dispose()
            {Target?.Dispose();_binding?.Dispose();Effects.Dispose();if(_started&&Body!=null){Body.linearVelocity=Vector2.zero;Body.simulated=false;}if(_started)Colorize(_baseColor);_started=false;}
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCombatScene(PlayerContext context,TopdownSnapshot restore=null,IPlayerCombatRules rules=null)
        {
            _player=context;
            if(!context.Scheme.CombatEnabled)return;
            _query=new PlayerHitQuery(context.Controller.gameObject.scene);
            try
            {
                _playerTarget=_batch.Register(context.Controller.GetComponent<Entity2D>(),"player",1,context.HitReceiver);
                var declarations=new List<TopdownCombatActor>();
                foreach(var root in context.Controller.gameObject.scene.GetRootGameObjects())
                    foreach(var source in root.GetComponentsInChildren<TopdownCombatActor>(true))
                        if(source.isActiveAndEnabled)declarations.Add(source);
                declarations.Sort((a,b)=>string.CompareOrdinal(a.StableId,b.StableId));
                if(restore!=null&&restore.ActorCount!=declarations.Count)throw new InvalidOperationException("世界角色装配集合不匹配。");
                for(var i=0;i<declarations.Count;i++)_actors.Add(new Actor(declarations[i],_batch,context.Scheme,restore?.GetActor(i),rules));
            }
            catch{Dispose();throw;}
        }
        internal void Start(){if(_disposed||_started)throw new InvalidOperationException("战斗会话已启动或失效。");foreach(var actor in _actors)actor.Start();_started=true;}
        internal void ValidateSources(){foreach(var actor in _actors)if(actor.Source==null||!actor.Source.isActiveAndEnabled||!actor.Target.IsValid)throw new InvalidOperationException("战斗候选对象已失效。");}
        internal TopdownActorRecord[] CaptureProgress()
        {
            ValidateSources();var result=new TopdownActorRecord[_actors.Count];
            for(var i=0;i<result.Length;i++){var a=_actors[i];result[i]=new TopdownActorRecord(a.StableId,a.Health.Current,a.Body.position);}return result;
        }
        internal PlayerCombatActorView[] Capture()
        {
            var result=new PlayerCombatActorView[_actors.Count];
            for(var i=0;i<result.Length;i++){var actor=_actors[i];result[i]=new PlayerCombatActorView(actor.StableId,actor.Health.Current,actor.Health.Maximum,actor.Phase,actor.Body==null?Vector2.zero:actor.Body.position);}
            return result;
        }
        internal void Tick(float dt){if(_disposed||!_started)return;foreach(var actor in _actors)actor.Effects.Tick(dt);}
        internal void Step(float dt)
        {
            if(_disposed||!_started||!_player.Scheme.CombatEnabled)return;
            foreach(var actor in _actors)if(actor.Target.IsValid)actor.Step(_player,dt);
            var weapon=_player.Weapons.Current;
            if(weapon?.Context.Emission==true&&!_player.IsDead)
            {
                var context=weapon.Context;var spec=weapon.Spec;var hit=spec.Hit(context.Direction);
                if(spec.Mode==Weapons.WeaponHitMode.Melee)_query.Query(spec.Offset,spec.Size,context.Direction,_player.StepFrom,_player.StepTo,collider=>
                {
                    var entity=Entity2D.Resolve(collider);
                    if(!_batch.TryGet(entity,out var target)||target.Team==_playerTarget.Team||!context.HitLedger.Add(entity))return;
                    _batch.Collect(_playerTarget,context.AttackInstance,spec.Id,target,hit);
                });
                else
                {
                    // 即时射线示例：从角色根起算，先截断实体墙，再选最近有效敌人；不穿墙或重复伤害多hurtbox。
                    var rays=_query.Ray(_player.StepFrom,context.Direction,spec.Range);var distance=spec.Range;
                    foreach(var ray in rays)
                        if(ray.collider!=null&&Entity2D.Resolve(ray.collider)!=_playerTarget.Entity&&!ray.collider.isTrigger&&
                            (_player.Scheme.Obstacles.value&(1<<ray.collider.gameObject.layer))!=0)distance=Mathf.Min(distance,ray.distance);
                    var wallDistance=distance;EntityHitBatch.Target nearest=null;
                    foreach(var ray in rays)
                    {
                        if(ray.collider==null||ray.distance>=wallDistance||ray.distance>distance||!_batch.TryGet(Entity2D.Resolve(ray.collider),out var target)||target.Team==_playerTarget.Team||
                            ray.distance==distance&&nearest!=null&&string.CompareOrdinal(target.Id,nearest.Id)>=0)continue;
                        distance=ray.distance;nearest=target;
                    }
                    weapon.TracerDistance=distance;
                    if(nearest!=null&&context.HitLedger.Add(nearest.Entity))_batch.Collect(_playerTarget,context.AttackInstance,spec.Id,nearest,hit);
                }
            }
            foreach(var actor in _actors)
            {
                if(!actor.Emission||!actor.Target.IsValid)continue;
                var hit=actor.Hit();var ledger=new HashSet<Entity2D>();
                _query.Query(new Vector2(.65f,0),new Vector2(1.1f,1),actor.Direction,actor.From,actor.To,collider=>
                {
                    var entity=Entity2D.Resolve(collider);
                    if(!_batch.TryGet(entity,out var target)||target.Team==actor.Target.Team||!ledger.Add(entity))return;
                    _batch.Collect(actor.Target,actor.Attack,"strike",target,hit);
                });
            }
            _batch.Resolve();
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            foreach(var actor in _actors)actor.Dispose();_actors.Clear();_batch.Dispose();_query?.Dispose();
        }
        #endregion
    }
}
