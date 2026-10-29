using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.LaneDefense
{
    public enum BattlePhase { Ready, Running, Intermission, Won, Lost, Disposed }
    public enum PlacementRejection { None, SessionExpired, DuplicateCommand, NotInteractive, PhaseDenied, UnknownCard, OutOfBounds, Unbuildable, Occupied, ResourceInsufficient, Cooldown, LimitReached, PolicyDenied, CapacityReached }
    public enum RemovalReason { Demolished, Killed, Restart, SceneExit }
    public enum EnemyEndReason { Killed, Escaped, Disposed }
    public enum ProjectileEndReason { Hit, Lifetime, Bounds, PolicyConsumed, BattleEnded, Disposed }
    public enum HitOutcome { Miss, Blocked, Damaged, Killed }
    public enum ProjectileStepResult { Continue, Consume }

    public readonly struct EntityKey : IEquatable<EntityKey>
    {
        public long Session {get;}
        public long Id {get;}
        public EntityKey(long session,long id) {Session=session;Id=id;}
        public bool Equals(EntityKey other)=>Session==other.Session&&Id==other.Id;
        public override bool Equals(object other)=>other is EntityKey key&&Equals(key);
        public override int GetHashCode()=>HashCode.Combine(Session,Id);
    }
    public readonly struct PlacementCommand
    {
        public long Session {get;}
        public long CommandId {get;}
        public string UnitId {get;}
        public Cell Cell {get;}
        public PlacementCommand(long session,long commandId,string unitId,Cell cell)
        {Session=session;CommandId=commandId;UnitId=unitId;Cell=cell;}
    }
    public readonly struct PlacementResult
    {
        public PlacementRejection Rejection {get;}
        public EntityKey Entity {get;}
        public bool Success=>Rejection==PlacementRejection.None;
        public PlacementResult(PlacementRejection reason,EntityKey entity=default) {Rejection=reason;Entity=entity;}
    }
    public readonly struct BattleEntity
    {
        public EntityKey Key {get;}
        public string DefinitionId {get;}
        public int Lane {get;}
        public Vector2 Position {get;}
        public int Health {get;}
        public int MaxHealth {get;}
        public BattleEntity(EntityKey key,string id,int lane,Vector2 position,int health,int maxHealth)
        {Key=key;DefinitionId=id;Lane=lane;Position=position;Health=health;MaxHealth=maxHealth;}
    }
    public readonly struct ProjectileSnapshot
    {
        public EntityKey Key {get;}
        public EntityKey Source {get;}
        public Vector2 Position {get;}
        public Vector2 Velocity {get;}
        public int Damage {get;}
        public float RemainingLifetime {get;}
        internal ProjectileSnapshot(EntityKey key,EntityKey source,Vector2 position,Vector2 velocity,int damage,float ttl)
        {Key=key;Source=source;Position=position;Velocity=velocity;Damage=damage;RemainingLifetime=ttl;}
    }
    public readonly struct PlacementQuery
    {
        public UnitDefinition Unit {get;}
        public Cell Cell {get;}
        public BattlePhase Phase {get;}
        public int Resource {get;}
        internal PlacementQuery(UnitDefinition unit,Cell cell,BattlePhase phase,int resource)
        {Unit=unit;Cell=cell;Phase=phase;Resource=resource;}
    }
    public readonly struct AttackQuery
    {
        public UnitDefinition Definition {get;}
        public BattleEntity Attacker {get;}
        public IReadOnlyList<BattleEntity> Enemies {get;}
        internal AttackQuery(UnitDefinition definition,BattleEntity attacker,IReadOnlyList<BattleEntity> enemies)
        {Definition=definition;Attacker=attacker;Enemies=enemies;}
    }
    public readonly struct HitRequest
    {
        public ProjectileSnapshot Projectile {get;}
        public BattleEntity Target {get;}
        internal HitRequest(ProjectileSnapshot projectile,BattleEntity target) {Projectile=projectile;Target=target;}
    }
    public readonly struct HitDecision
    {
        public bool Blocked {get;}
        public int Damage {get;}
        public HitDecision(int damage,bool blocked=false) {Damage=Math.Max(0,damage);Blocked=blocked;}
    }
    public readonly struct BattleResult
    {
        public string Id {get;}
        public string LevelId {get;}
        public BattlePhase Phase {get;}
        public int BaseHealth {get;}
        public int Kills {get;}
        public int Escapes {get;}
        internal BattleResult(long session,string level,BattlePhase phase,int health,int kills,int escapes)
        {Id=level+":"+session;LevelId=level;Phase=phase;BaseHealth=health;Kills=kills;Escapes=escapes;}
    }

    public interface IPlacementRule { bool Allows(in PlacementQuery query); }
    public interface IAttackPolicy { bool TryGetVelocity(in AttackQuery query,out Vector2 velocity); }
    public interface IHitRule { HitDecision Evaluate(in HitRequest request); }
    /// <summary>会话为每个弹丸每步调用一次。替换后默认处理器不运行。禁止外部再次更新。</summary>
    public interface IProjectilePolicy { ProjectileStepResult Step(ProjectileContext context,in ProjectileSnapshot projectile,float delta); }

    public sealed class LaneDefensePolicies
    {
        public IPlacementRule Placement {get;}
        public IAttackPolicy Attack {get;}
        public IProjectilePolicy Projectile {get;}
        public IHitRule Hit {get;}
        public LaneDefensePolicies(IPlacementRule placement=null,IAttackPolicy attack=null,IProjectilePolicy projectile=null,IHitRule hit=null)
        {Placement=placement;Attack=attack??new SameLaneAttackPolicy();Projectile=projectile??new StraightProjectilePolicy();Hit=hit??new BasicHitRule();}
    }
    internal sealed class SameLaneAttackPolicy : IAttackPolicy
    {
        public bool TryGetVelocity(in AttackQuery query,out Vector2 velocity)
        {
            velocity=Vector2.right*query.Definition.ProjectileSpeed;
            foreach(var e in query.Enemies)
                if(e.Lane==query.Attacker.Lane&&e.Position.x>=query.Attacker.Position.x&&e.Position.x-query.Attacker.Position.x<=query.Definition.Range)return true;
            return false;
        }
    }
    internal sealed class BasicHitRule : IHitRule
    { public HitDecision Evaluate(in HitRequest request)=>new HitDecision(request.Projectile.Damage); }
    internal sealed class StraightProjectilePolicy : IProjectilePolicy
    {
        public ProjectileStepResult Step(ProjectileContext context,in ProjectileSnapshot projectile,float delta)
        {
            var end=projectile.Position+projectile.Velocity*delta;
            if(context.TrySweep(projectile.Position,end,.22f,out var target))
            {context.SubmitHit(target);return ProjectileStepResult.Consume;}
            context.Move(end,projectile.Velocity);return ProjectileStepResult.Continue;
        }
    }

    /// <summary>仅当前同步 Step 内有效的能力。保留后调用、对其它弹丸命中、跨会话提交均被拒绝。</summary>
    public sealed class ProjectileContext
    {
        private readonly BattleSession _session;
        private readonly EntityKey _projectile;
        internal bool Active;
        internal ProjectileContext(BattleSession session,EntityKey projectile) {_session=session;_projectile=projectile;}
        public bool Move(Vector2 position,Vector2 velocity)=>Active&&_session.MoveProjectile(_projectile,position,velocity);
        public bool TrySweep(Vector2 from,Vector2 to,float radius,out EntityKey target)
        {target=default;return Active&&_session.Sweep(_projectile,from,to,radius,out target);}
        public HitOutcome SubmitHit(EntityKey target)=>Active?_session.SubmitHit(_projectile,target):HitOutcome.Miss;
        public EntityKey Spawn(Vector2 position,Vector2 velocity,int damage,float lifetime)
            =>Active?_session.SpawnDerived(_projectile,position,velocity,damage,lifetime):default;
    }
}
