using System;
using System.Collections.Generic;
using System.Threading;
using Ember.Basic;
using UnityEngine;

namespace Game.LaneDefense
{
    /// <summary>权威模拟及唯一实体所有者。没有 GameObject、UI 或 Physics 回调依赖。</summary>
    public sealed class BattleSession : IDisposable
    {
        #region 内部参数
        private sealed class Unit
        {internal long Id; internal UnitDefinition Definition; internal Cell Cell; internal int Health; internal float Attack;}
        private sealed class Enemy
        {internal long Id; internal EnemyDefinition Definition; internal int Lane; internal Vector2 Position; internal int Health; internal float Attack; internal long Target;}
        private sealed class Projectile
        {internal long Id; internal EntityKey Source; internal Vector2 Position,Velocity; internal int Damage; internal float Ttl; internal readonly HashSet<long> Hits=new(); internal ProjectileContext Context;}
        private static long _generation;
        private readonly LaneDefensePolicies _policies;
        private readonly Dictionary<string,UnitDefinition> _cards=new(StringComparer.Ordinal);
        private readonly Dictionary<string,EnemyDefinition> _enemyDefinitions=new(StringComparer.Ordinal);
        private readonly Dictionary<long,Unit> _units=new();
        private readonly Dictionary<long,Enemy> _enemies=new();
        private readonly Dictionary<long,Projectile> _projectiles=new();
        private readonly Dictionary<string,float> _cooldowns=new(StringComparer.Ordinal);
        private readonly HashSet<long> _credits=new();
        private readonly List<long> _ids=new();
        private readonly List<BattleEntity> _targets=new();
        private readonly List<Action> _notifications=new();
        private long _nextId,_lastCommand,_issuedCommand;
        private bool _started,_advancing,_committing;
        private float _waveTime;
        private int _spawnIndex;
        private long _activeProjectile;
        private const int MAX_ENTITIES=2048;
        private const int MAX_PROJECTILES=1024;
        public long Generation {get;}
        public LaneLevelSnapshot Level {get;}
        public BoardModel Board {get;}
        public BattlePhase Phase {get;private set;}=BattlePhase.Ready;
        public bool Paused {get;private set;}
        public float Time {get;private set;}
        public int Resource {get;private set;}
        public int BaseHealth {get;private set;}
        public int WaveIndex {get;private set;}=-1;
        public int EnemyCount=>_enemies.Count;
        public int UnitCount=>_units.Count;
        public int ProjectileCount=>_projectiles.Count;
        public int Kills {get;private set;}
        public int Escapes {get;private set;}
        public BattleResult? Result {get;private set;}
        public event Action<BattleResult> Completed;
        public event Action<EntityKey,RemovalReason> UnitRemoved;
        public event Action<EntityKey,EnemyEndReason> EnemyRemoved;
        public event Action<EntityKey,ProjectileEndReason> ProjectileRemoved;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private EntityKey Key(long id)=>new EntityKey(Generation,id);
        private BattleEntity View(Unit u)=>new BattleEntity(Key(u.Id),u.Definition.Id,u.Cell.Row,Board.Center(u.Cell),u.Health,u.Definition.Health);
        private BattleEntity View(Enemy e)=>new BattleEntity(Key(e.Id),e.Definition.Id,e.Lane,e.Position,e.Health,e.Definition.Health);
        private ProjectileSnapshot View(Projectile p)=>new ProjectileSnapshot(Key(p.Id),p.Source,p.Position,p.Velocity,p.Damage,p.Ttl);
        private bool Current(EntityKey key)=>key.Session==Generation&&key.Id>0&&Phase!=BattlePhase.Disposed;
        private bool Interactive=>_started&&!Paused&&Phase!=BattlePhase.Won&&Phase!=BattlePhase.Lost&&Phase!=BattlePhase.Disposed;
        private void Notify<T>(Action<T> callback,T value)
        {
            if(_advancing){if(callback!=null)_notifications.Add(()=>Notify(callback,value));return;}
            if(callback==null)return;
            foreach(Action<T> listener in callback.GetInvocationList())
                try{listener(value);}catch(Exception e){EmberDebug.LogError("Game.LaneDefense","Observer failed: "+e);}
        }
        private void Notify<T1,T2>(Action<T1,T2> callback,T1 first,T2 second)
        {
            if(_advancing){if(callback!=null)_notifications.Add(()=>Notify(callback,first,second));return;}
            if(callback==null)return;
            foreach(Action<T1,T2> listener in callback.GetInvocationList())
                try{listener(first,second);}catch(Exception e){EmberDebug.LogError("Game.LaneDefense","Observer failed: "+e);}
        }
        private PlacementRejection Check(string id,Cell cell)
        {
            if(!Interactive||_advancing||_committing)return PlacementRejection.NotInteractive;
            if(Phase==BattlePhase.Running&&!Level.BuildDuringWave)return PlacementRejection.PhaseDenied;
            if(!_cards.TryGetValue(id??"",out var card))return PlacementRejection.UnknownCard;
            if(!Board.Contains(cell))return PlacementRejection.OutOfBounds;
            if(!Board.IsBuildable(cell))return PlacementRejection.Unbuildable;
            if(Board.Occupant(cell)!=0)return PlacementRejection.Occupied;
            if(Resource<card.Cost)return PlacementRejection.ResourceInsufficient;
            if(CooldownRemaining(id)>0)return PlacementRejection.Cooldown;
            int count=0;foreach(var u in _units.Values)if(u.Definition.Id==id)count++;
            if(count>=card.Limit)return PlacementRejection.LimitReached;
            if(_units.Count>=MAX_ENTITIES)return PlacementRejection.CapacityReached;
            if(_policies.Placement!=null&&!_policies.Placement.Allows(new PlacementQuery(card,cell,Phase,Resource)))return PlacementRejection.PolicyDenied;
            return PlacementRejection.None;
        }
        private void Step(float dt)
        {
            Time+=dt;
            if(Phase!=BattlePhase.Running)return;
            _waveTime+=dt;
            var wave=Level.Waves[WaveIndex];
            while(_spawnIndex<wave.Spawns.Count&&wave.Spawns[_spawnIndex].Time<=_waveTime)
            {
                if(_enemies.Count>=MAX_ENTITIES)throw new InvalidOperationException("Enemy budget exceeded; configuration cannot complete.");
                var s=wave.Spawns[_spawnIndex++];var def=_enemyDefinitions[s.EnemyId];var id=++_nextId;
                _enemies.Add(id,new Enemy{Id=id,Definition=def,Lane=s.Lane,Position=new Vector2(Level.Columns+.5f,Level.Rows-s.Lane-.5f),Health=def.Health});
            }
            _ids.Clear();_ids.AddRange(_enemies.Keys);
            for(int i=0;i<_ids.Count;i++)
            {
                if(!_enemies.TryGetValue(_ids[i],out var e))continue;
                Unit blocker=null;
                float next=e.Position.x-e.Definition.Speed*dt;
                foreach(var u in _units.Values)
                {
                    float x=u.Cell.Column+.5f;
                    if(u.Cell.Row==e.Lane&&x<=e.Position.x+.001f&&x+.55f>=next&&(blocker==null||u.Cell.Column>blocker.Cell.Column))blocker=u;
                }
                if(blocker!=null)
                {
                    e.Position=new Vector2(Math.Max(e.Position.x,blocker.Cell.Column+1.05f),e.Position.y);
                    if(e.Target!=blocker.Id){e.Target=blocker.Id;e.Attack=0;}
                    e.Attack-=dt;
                    if(e.Attack<=0){e.Attack+=e.Definition.AttackInterval;blocker.Health-=e.Definition.Damage;if(blocker.Health<=0)RemoveUnitInternal(blocker,RemovalReason.Killed);}
                }
                else
                {
                    e.Target=0;e.Position=new Vector2(next,e.Position.y);
                    if(next<0){BaseHealth=(int)Math.Max(int.MinValue,(long)BaseHealth-e.Definition.BaseDamage);RemoveEnemy(e,EnemyEndReason.Escaped);}
                }
            }
            _targets.Clear();foreach(var e in _enemies.Values)_targets.Add(View(e));
            _targets.Sort((a,b)=>{int d=a.Position.x.CompareTo(b.Position.x);return d!=0?d:a.Key.Id.CompareTo(b.Key.Id);});
            foreach(var u in _units.Values)
            {
                if(!u.Definition.Shooter)continue;
                u.Attack=Math.Max(0,u.Attack-dt);
                if(u.Attack<=0&&_policies.Attack.TryGetVelocity(new AttackQuery(u.Definition,View(u),_targets.AsReadOnly()),out var velocity))
                {
                    if(SpawnProjectile(Key(u.Id),Board.Center(u.Cell),velocity,u.Definition.Damage,u.Definition.ProjectileLifetime).Id!=0)u.Attack=u.Definition.AttackInterval;
                }
            }
            _ids.Clear();_ids.AddRange(_projectiles.Keys);
            for(int i=0;i<_ids.Count;i++)
            {
                if(!_projectiles.TryGetValue(_ids[i],out var p))continue;
                float moveTime=Math.Min(dt,p.Ttl);p.Ttl-=dt;
                _activeProjectile=p.Id;p.Context.Active=true;
                ProjectileStepResult action;
                try{action=_policies.Projectile.Step(p.Context,View(p),moveTime);}
                finally{p.Context.Active=false;_activeProjectile=0;}
                if(!_projectiles.ContainsKey(p.Id))continue;
                if(action==ProjectileStepResult.Consume)RemoveProjectile(p,p.Hits.Count>0?ProjectileEndReason.Hit:ProjectileEndReason.PolicyConsumed);
                else if(p.Ttl<=0)RemoveProjectile(p,ProjectileEndReason.Lifetime);
                else if(p.Position.x<-.75f||p.Position.x>Level.Columns+1.5f||p.Position.y<-.75f||p.Position.y>Level.Rows+.75f)RemoveProjectile(p,ProjectileEndReason.Bounds);
            }
            ResolveWave();
        }
        private void ResolveWave()
        {
            if(BaseHealth<=0){Finish(BattlePhase.Lost);return;}
            if(Phase==BattlePhase.Running&&_spawnIndex==Level.Waves[WaveIndex].Spawns.Count&&_enemies.Count==0)
            {if(WaveIndex==Level.Waves.Count-1)Finish(BattlePhase.Won);else Phase=BattlePhase.Intermission;}
        }
        private void Finish(BattlePhase phase)
        {
            if(Result.HasValue)return;
            Phase=phase;Paused=false;
            foreach(var p in new List<Projectile>(_projectiles.Values))RemoveProjectile(p,ProjectileEndReason.BattleEnded);
            Result=new BattleResult(Generation,Level.Id,phase,BaseHealth,Kills,Escapes);
            Notify(Completed,Result.Value);
        }
        private void RemoveUnitInternal(Unit unit,RemovalReason reason)
        {
            if(!_units.Remove(unit.Id))return;
            Board.Clear(unit.Cell,unit.Id);
            if(reason==RemovalReason.Demolished)Resource=(int)Math.Min(Level.ResourceCap,(long)Resource+(long)Math.Floor(unit.Definition.Cost*(double)Level.DemolishRefund));
            Notify(UnitRemoved,Key(unit.Id),reason);
        }
        private void RemoveEnemy(Enemy enemy,EnemyEndReason reason)
        {
            if(!_enemies.Remove(enemy.Id))return;
            if(reason==EnemyEndReason.Killed)Kills++;else if(reason==EnemyEndReason.Escaped)Escapes++;
            Notify(EnemyRemoved,Key(enemy.Id),reason);
        }
        private void RemoveProjectile(Projectile p,ProjectileEndReason reason)
        {
            if(!_projectiles.Remove(p.Id))return;
            p.Context.Active=false;
            Notify(ProjectileRemoved,Key(p.Id),reason);
        }
        private EntityKey SpawnProjectile(EntityKey source,Vector2 position,Vector2 velocity,int damage,float ttl)
        {
            if(!Current(source)||Phase!=BattlePhase.Running||_projectiles.Count>=MAX_PROJECTILES||damage<0||!Valid(position)||!Valid(velocity)||!float.IsFinite(ttl)||ttl<=0)return default;
            long id=++_nextId;
            var p=new Projectile{Id=id,Source=source,Position=position,Velocity=velocity,Damage=damage,Ttl=ttl};
            p.Context=new ProjectileContext(this,Key(id));_projectiles.Add(id,p);return Key(id);
        }
        private bool Valid(Vector2 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y);
        private bool CanDrive(EntityKey key)=>Current(key)&&_activeProjectile==key.Id&&_advancing&&Phase==BattlePhase.Running&&_projectiles.ContainsKey(key.Id);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public BattleSession(LaneLevelSnapshot level,LaneDefensePolicies policies=null)
        {
            Level=level??throw new ArgumentNullException(nameof(level));_policies=policies??new LaneDefensePolicies();
            Generation=Interlocked.Increment(ref _generation);Board=new BoardModel(level);Resource=level.InitialResource;BaseHealth=level.BaseHealth;
            foreach(var u in level.Units)_cards.Add(u.Id,u);foreach(var e in level.Enemies)_enemyDefinitions.Add(e.Id,e);
        }
        public void Start() {if(Phase==BattlePhase.Disposed)throw new ObjectDisposedException(nameof(BattleSession));_started=true;}
        public bool StartWave()
        {
            if(!Interactive||_advancing||(Phase!=BattlePhase.Ready&&Phase!=BattlePhase.Intermission))return false;
            if(Level.Waves.Count==0){Finish(BattlePhase.Won);return true;}
            WaveIndex++;_spawnIndex=0;_waveTime=0;Phase=BattlePhase.Running;ResolveWave();return true;
        }
        public void SetPaused(bool paused) {if(_started&&Phase!=BattlePhase.Disposed&&!Result.HasValue)Paused=paused;}
        public float CooldownRemaining(string card)=>_cooldowns.TryGetValue(card,out float until)?Math.Max(0,until-Time):0;
        public PlacementRejection Preview(string card,Cell cell)=>Check(card,cell);
        /// <summary>输入适配器共享编号分配；外部提交过更大的编号时继续向前，不锁死后续点击。</summary>
        public long NextCommandId()
        {if(Phase==BattlePhase.Disposed)throw new ObjectDisposedException(nameof(BattleSession));_issuedCommand=Math.Max(_issuedCommand,_lastCommand)+1;return _issuedCommand;}
        public PlacementRejection CardRejection(string id)
        {
            if(!Interactive||_advancing||_committing)return PlacementRejection.NotInteractive;
            if(Phase==BattlePhase.Running&&!Level.BuildDuringWave)return PlacementRejection.PhaseDenied;
            if(!_cards.TryGetValue(id??"",out var card))return PlacementRejection.UnknownCard;
            if(Resource<card.Cost)return PlacementRejection.ResourceInsufficient;
            if(CooldownRemaining(id)>0)return PlacementRejection.Cooldown;
            int count=0;foreach(var u in _units.Values)if(u.Definition.Id==id)count++;
            return count>=card.Limit?PlacementRejection.LimitReached:_units.Count>=MAX_ENTITIES?PlacementRejection.CapacityReached:PlacementRejection.None;
        }
        public PlacementResult Place(in PlacementCommand command)
        {
            if(command.Session!=Generation||Phase==BattlePhase.Disposed)return new PlacementResult(PlacementRejection.SessionExpired);
            if(command.CommandId<=_lastCommand||command.CommandId<=0)return new PlacementResult(PlacementRejection.DuplicateCommand);
            _lastCommand=command.CommandId;
            var rejected=Check(command.UnitId,command.Cell);if(rejected!=PlacementRejection.None)return new PlacementResult(rejected);
            // No external object creation or callbacks inside commit. Presentation consumes the committed model later.
            var definition=_cards[command.UnitId];var id=++_nextId;
            var unit=new Unit{Id=id,Definition=definition,Cell=command.Cell,Health=definition.Health};
            _committing=true;
            try{_units.Add(id,unit);_cooldowns[definition.Id]=Time+definition.Cooldown;Board.Occupy(command.Cell,id);Resource-=definition.Cost;}
            finally{_committing=false;}
            return new PlacementResult(PlacementRejection.None,Key(id));
        }
        public bool RemoveUnit(EntityKey key,RemovalReason reason=RemovalReason.Demolished)
        {
            if(!Interactive||_advancing||!Current(key)||reason!=RemovalReason.Demolished||!_units.TryGetValue(key.Id,out var unit))return false;
            RemoveUnitInternal(unit,reason);return true;
        }
        public bool Credit(long session,long eventId,int amount,string source)
        {
            if(session!=Generation||!Interactive||_advancing||eventId<=0||amount<=0||string.IsNullOrWhiteSpace(source)||!_credits.Add(eventId))return false;
            Resource=(int)Math.Min(Level.ResourceCap,(long)Resource+amount);return true;
        }
        public void Advance(float delta)
        {
            if(!float.IsFinite(delta)||delta<0||delta>1)throw new ArgumentOutOfRangeException(nameof(delta));
            if(!Interactive||_advancing)return;
            _advancing=true;
            try{float remaining=delta;while(remaining>0&&Interactive){float step=Math.Min(.02f,remaining);Step(step);remaining-=step;}}
            catch{Dispose();throw;}
            finally
            {
                _advancing=false;
                var notifications=_notifications.ToArray();_notifications.Clear();
                foreach(var notify in notifications)notify();
            }
        }
        public void CopyUnits(List<BattleEntity> output) {output.Clear();foreach(var u in _units.Values)output.Add(View(u));}
        public void CopyEnemies(List<BattleEntity> output) {output.Clear();foreach(var e in _enemies.Values)output.Add(View(e));}
        public void CopyProjectiles(List<ProjectileSnapshot> output) {output.Clear();foreach(var p in _projectiles.Values)output.Add(View(p));}
        internal bool MoveProjectile(EntityKey key,Vector2 position,Vector2 velocity)
        {if(!CanDrive(key)||!Valid(position)||!Valid(velocity))return false;var p=_projectiles[key.Id];p.Position=position;p.Velocity=velocity;return true;}
        internal EntityKey SpawnDerived(EntityKey parent,Vector2 position,Vector2 velocity,int damage,float ttl)
            =>CanDrive(parent)?SpawnProjectile(_projectiles[parent.Id].Source,position,velocity,damage,ttl):default;
        internal bool Sweep(EntityKey projectile,Vector2 from,Vector2 to,float radius,out EntityKey target)
        {
            target=default;if(!CanDrive(projectile)||!Valid(from)||!Valid(to)||!float.IsFinite(radius)||radius<0)return false;
            var segment=to-from;float length=segment.sqrMagnitude;float best=float.PositiveInfinity;long id=0;
            foreach(var e in _enemies.Values)
            {
                if(_projectiles[projectile.Id].Hits.Contains(e.Id))continue;
                float t=length<=0?0:Mathf.Clamp01(Vector2.Dot(e.Position-from,segment)/length);
                if((e.Position-(from+t*segment)).sqrMagnitude>radius*radius)continue;
                // Swept circle first-entry time; stable ID resolves identical distances.
                float entry=t;
                if(length>0){float perpendicular=(e.Position-(from+t*segment)).sqrMagnitude;entry=Math.Max(0,t-Mathf.Sqrt(Math.Max(0,radius*radius-perpendicular)/length));}
                if(entry<best||(entry==best&&e.Id<id)){best=entry;id=e.Id;}
            }
            if(id==0)return false;target=Key(id);return true;
        }
        internal HitOutcome SubmitHit(EntityKey projectile,EntityKey target)
        {
            if(!CanDrive(projectile)||!Current(target)||!_enemies.TryGetValue(target.Id,out var enemy))return HitOutcome.Miss;
            var p=_projectiles[projectile.Id];if(!p.Hits.Add(target.Id))return HitOutcome.Miss;
            var decision=_policies.Hit.Evaluate(new HitRequest(View(p),View(enemy)));
            if(decision.Blocked||decision.Damage==0)return HitOutcome.Blocked;
            enemy.Health=(int)Math.Max(int.MinValue,(long)enemy.Health-decision.Damage);
            if(enemy.Health<=0){RemoveEnemy(enemy,EnemyEndReason.Killed);return HitOutcome.Killed;}
            return HitOutcome.Damaged;
        }
        public void Dispose()=>Dispose(RemovalReason.SceneExit);
        public void Dispose(RemovalReason reason)
        {
            if(Phase==BattlePhase.Disposed)return;
            Phase=BattlePhase.Disposed;_started=false;Paused=false;
            foreach(var p in new List<Projectile>(_projectiles.Values))RemoveProjectile(p,ProjectileEndReason.Disposed);
            foreach(var u in new List<Unit>(_units.Values))RemoveUnitInternal(u,reason==RemovalReason.Restart?reason:RemovalReason.SceneExit);
            foreach(var e in new List<Enemy>(_enemies.Values))RemoveEnemy(e,EnemyEndReason.Disposed);
            _cooldowns.Clear();_credits.Clear();Completed=null;UnitRemoved=null;EnemyRemoved=null;ProjectileRemoved=null;
        }
        #endregion
    }
}
