using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.LaneDefense.Tests
{
    public sealed class LaneDefenseTests
    {
        private readonly List<UnityEngine.Object> _assets=new();
        private readonly List<BattleSession> _sessions=new();
        private LaneDefenseLevel Level(int rows=2,int cols=4,int resource=500,int health=3,
            UnitDefinition unit=null,EnemyDefinition enemy=null,WaveDefinition[] waves=null,int[] blocked=null)
        {
            var l=ScriptableObject.CreateInstance<LaneDefenseLevel>();_assets.Add(l);
            l.Configure("test",rows,cols,new Vector2(1.3f,.8f),resource,health,
                new[]{unit??new UnitDefinition("s","Sentry",50,100,true,0,.1f,100,1000)},
                new[]{enemy??new EnemyDefinition("e",100,.1f)},waves??new[]{new WaveDefinition("w",new SpawnEntry("e",0,0))},blocked);
            return l;
        }
        private BattleSession Session(LaneDefenseLevel level=null,LaneDefensePolicies policies=null)
        {var s=new BattleSession((level??Level()).Freeze(),policies);_sessions.Add(s);s.Start();return s;}
        private PlacementResult Put(BattleSession s,int row=0,int col=0,long command=1)=>s.Place(new PlacementCommand(s.Generation,command,"s",new Cell(row,col)));
        private void Advance(BattleSession s,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*10);i++)s.Advance(.1f);}
        [TearDown] public void Cleanup(){foreach(var s in _sessions)s.Dispose();foreach(var a in _assets)UnityEngine.Object.DestroyImmediate(a);_sessions.Clear();_assets.Clear();}

        [Test] public void ProjectionRoundTrip_NonSquareAndOpenEdges()
        {
            var p=new BoardProjection(3,7,new Vector2(1.7f,.6f),new Vector2(-4,2));
            for(int r=0;r<3;r++)for(int c=0;c<7;c++){Assert.That(p.TryWorldToCell(p.ToWorld(new Vector2(c+.5f,3-r-.5f)),out var cell));Assert.That(cell,Is.EqualTo(new Cell(r,c)));}
            Assert.That(p.TryWorldToCell(p.ToWorld(new Vector2(-.001f,1)),out _),Is.False);
            Assert.That(p.TryWorldToCell(p.ToWorld(new Vector2(7,1)),out _),Is.False);
            Assert.That(p.TryWorldToCell(p.ToWorld(new Vector2(1,3)),out _),Is.False);
        }
        [Test] public void Freeze_IsolatedFromSourceConfiguration()
        {
            var l=Level();var s=Session(l);l.Configure("changed",1,1,Vector2.one,0,1,new[]{new UnitDefinition("new","New",0,1)},Array.Empty<EnemyDefinition>(),Array.Empty<WaveDefinition>());
            Assert.That(s.Level.Rows,Is.EqualTo(2));Assert.That(Put(s).Success);Assert.That(s.Resource,Is.EqualTo(450));
        }
        [Test] public void ValidationRejectsDuplicateIds_UnsortedAndMissingSpawns()
        {
            var l=Level(waves:new[]{new WaveDefinition("w",new SpawnEntry("missing",0,2),new SpawnEntry("e",2,1))});Assert.That(l.Validate().Count,Is.GreaterThan(0));Assert.Throws<InvalidOperationException>(()=>l.Freeze());
        }
        [Test] public void PreviewAndDuplicatePlacement_HaveNoExtraSideEffects()
        {
            var s=Session();for(int i=0;i<10;i++)Assert.That(s.Preview("s",new Cell(0,0)),Is.EqualTo(PlacementRejection.None));
            var one=Put(s);Assert.That(one.Success);Assert.That(Put(s).Rejection,Is.EqualTo(PlacementRejection.DuplicateCommand));
            Assert.That(s.Resource,Is.EqualTo(450));Assert.That(s.UnitCount,Is.EqualTo(1));Assert.That(s.Board.Occupant(new Cell(0,0)),Is.EqualTo(one.Entity.Id));
        }
        [TestCase(-1,0,PlacementRejection.OutOfBounds)]
        [TestCase(0,4,PlacementRejection.OutOfBounds)]
        [TestCase(0,1,PlacementRejection.Unbuildable)]
        public void PlacementRejected_LeavesEconomyAndBoard(int row,int col,PlacementRejection reason)
        {var s=Session(Level(blocked:new[]{1}));Assert.That(Put(s,row,col).Rejection,Is.EqualTo(reason));Assert.That(s.Resource,Is.EqualTo(500));Assert.That(s.UnitCount,Is.Zero);}
        [Test] public void PlacementChecksResourceCooldownOccupancyLimitAndPhase()
        {
            var s=Session(Level(resource:49));Assert.That(Put(s).Rejection,Is.EqualTo(PlacementRejection.ResourceInsufficient));
            var c=Session(Level(unit:new UnitDefinition("s","S",50,100,cooldown:2,limit:1)));Assert.That(Put(c).Success);
            Assert.That(Put(c,0,0,2).Rejection,Is.EqualTo(PlacementRejection.Occupied));Assert.That(Put(c,0,1,3).Rejection,Is.EqualTo(PlacementRejection.Cooldown));
            Advance(c,2.1f);Assert.That(Put(c,0,1,4).Rejection,Is.EqualTo(PlacementRejection.LimitReached));
            c.SetPaused(true);Assert.That(Put(c,0,2,5).Rejection,Is.EqualTo(PlacementRejection.NotInteractive));
        }
        private sealed class Deny : IPlacementRule {public bool Allows(in PlacementQuery query)=>false;}
        [Test] public void CustomPlacementRejection_IsAtomic()
        {var s=Session(policies:new LaneDefensePolicies(placement:new Deny()));Assert.That(Put(s).Rejection,Is.EqualTo(PlacementRejection.PolicyDenied));Assert.That(s.Resource,Is.EqualTo(500));Assert.That(s.UnitCount,Is.Zero);}
        [Test] public void RemovalReasonsRefundOnce_ExitDoesNotReward()
        {
            var s=Session();var key=Put(s).Entity;Assert.That(s.RemoveUnit(key,RemovalReason.Killed),Is.False);Assert.That(s.RemoveUnit(key));Assert.That(s.RemoveUnit(key),Is.False);Assert.That(s.Resource,Is.EqualTo(475));
            Put(s,0,1,2);s.Dispose(RemovalReason.Restart);Assert.That(s.Resource,Is.EqualTo(425));Assert.That(s.UnitCount,Is.Zero);
        }
        [Test] public void PauseFreezesAllCombatTimeAndCooldown()
        {var s=Session(Level(unit:new UnitDefinition("s","S",50,100,cooldown:2)));Put(s);s.StartWave();s.SetPaused(true);Advance(s,1);Assert.That(s.Time,Is.Zero);Assert.That(s.EnemyCount,Is.Zero);Assert.That(s.CooldownRemaining("s"),Is.EqualTo(2));s.SetPaused(false);s.Advance(.1f);Assert.That(s.Time,Is.GreaterThan(0));}
        [Test] public void SameLaneDefault_DoesNotHitAdjacentLane()
        {var s=Session(Level(waves:new[]{new WaveDefinition("w",new SpawnEntry("e",1,0))}));Put(s);s.StartWave();Advance(s,2);Assert.That(s.Kills,Is.Zero);Assert.That(s.ProjectileCount,Is.Zero);Assert.That(s.EnemyCount,Is.EqualTo(1));}
        [Test] public void HighSpeedSweep_HitsOnceAndRecyclesOnce()
        {var s=Session();Put(s);int recycled=0,results=0;s.ProjectileRemoved+=(key,reason)=>recycled++;s.Completed+=r=>results++;s.StartWave();Advance(s,1);Assert.That(s.Kills,Is.EqualTo(1));Assert.That(recycled,Is.EqualTo(1));Assert.That(results,Is.EqualTo(1));Assert.That(s.Phase,Is.EqualTo(BattlePhase.Won));}
        private sealed class Diagonal : IAttackPolicy
        {public bool TryGetVelocity(in AttackQuery q,out Vector2 velocity){velocity=default;if(q.Enemies.Count==0)return false;velocity=(q.Enemies[0].Position-q.Attacker.Position).normalized*1000;return true;}}
        [Test] public void CrossLanePolicy_UsesTwoDimensionalSweep()
        {var s=Session(Level(waves:new[]{new WaveDefinition("w",new SpawnEntry("e",1,0))}),new LaneDefensePolicies(attack:new Diagonal()));Put(s);s.StartWave();Advance(s,1);Assert.That(s.Kills,Is.EqualTo(1));Assert.That(s.Phase,Is.EqualTo(BattlePhase.Won));}
        private sealed class BlockHit : IHitRule {public HitDecision Evaluate(in HitRequest r)=>new HitDecision(100,true);}
        [Test] public void HitInterception_DoesNotApplyDefaultDamage()
        {var s=Session(policies:new LaneDefensePolicies(hit:new BlockHit()));Put(s);s.StartWave();Advance(s,1);var enemies=new List<BattleEntity>();s.CopyEnemies(enemies);Assert.That(enemies[0].Health,Is.EqualTo(100));Assert.That(s.Kills,Is.Zero);}
        private sealed class DoubleHit : IProjectilePolicy
        {
            internal ProjectileContext Saved;internal HitOutcome First,Second;internal int Calls;
            public ProjectileStepResult Step(ProjectileContext c,in ProjectileSnapshot p,float dt)
            {Saved=c;Calls++;if(c.TrySweep(p.Position,p.Position+Vector2.right*100,.25f,out var target)){First=c.SubmitHit(target);Second=c.SubmitHit(target);}return ProjectileStepResult.Consume;}
        }
        [Test] public void ReplacementDriver_IsUnique_HitDedup_AndLateContextIsInvalid()
        {var driver=new DoubleHit();var s=Session(Level(enemy:new EnemyDefinition("e",500)),new LaneDefensePolicies(projectile:driver));Put(s);s.StartWave();s.Advance(.02f);var es=new List<BattleEntity>();s.CopyEnemies(es);Assert.That(driver.Calls,Is.EqualTo(1));Assert.That(es[0].Health,Is.EqualTo(400));Assert.That(driver.First,Is.EqualTo(HitOutcome.Damaged));Assert.That(driver.Second,Is.EqualTo(HitOutcome.Miss));Assert.That(driver.Saved.Move(Vector2.zero,Vector2.zero),Is.False);Assert.That(driver.Saved.SubmitHit(es[0].Key),Is.EqualTo(HitOutcome.Miss));}
        private sealed class Inert : IProjectilePolicy {public ProjectileStepResult Step(ProjectileContext c,in ProjectileSnapshot p,float dt)=>ProjectileStepResult.Continue;}
        [Test] public void Lifetime_StillEnforcedWithReplacementDriver()
        {var s=Session(Level(unit:new UnitDefinition("s","S",0,100,interval:10,lifetime:.05f)),new LaneDefensePolicies(projectile:new Inert()));Put(s);int ttl=0;s.ProjectileRemoved+=(k,r)=>{if(r==ProjectileEndReason.Lifetime)ttl++;};s.StartWave();Advance(s,.2f);Assert.That(ttl,Is.EqualTo(1));Assert.That(s.ProjectileCount,Is.Zero);}
        [Test] public void EnemyBlockerDeath_ResumesMovement()
        {var s=Session(Level(cols:2,unit:new UnitDefinition("s","Barrier",0,10,false),enemy:new EnemyDefinition("e",100,2,20,.1f)));Put(s,0,1);s.StartWave();Advance(s,1.5f);Assert.That(s.UnitCount,Is.Zero);Assert.That(s.Board.Occupant(new Cell(0,1)),Is.Zero);Assert.That(s.Escapes,Is.EqualTo(1));}
        [Test] public void EmptyWavesAndDuplicateStart_AreDefined()
        {var s=Session(Level(waves:new[]{new WaveDefinition("empty"),new WaveDefinition("next",new SpawnEntry("e",0,10))}));Assert.That(s.StartWave());Assert.That(s.Phase,Is.EqualTo(BattlePhase.Intermission));Assert.That(s.StartWave());Assert.That(s.StartWave(),Is.False);Advance(s,1);Assert.That(s.Phase,Is.EqualTo(BattlePhase.Running));Assert.That(s.EnemyCount,Is.Zero);}
        [Test] public void LastSpawnCanDieFirst_WithoutPrematureCompletion()
        {var s=Session(Level(waves:new[]{new WaveDefinition("w",new SpawnEntry("e",1,0),new SpawnEntry("e",0,1))}));Put(s);s.StartWave();Advance(s,2);Assert.That(s.Kills,Is.EqualTo(1));Assert.That(s.EnemyCount,Is.EqualTo(1));Assert.That(s.Phase,Is.EqualTo(BattlePhase.Running));}
        [Test] public void EscapeAndWaveClearSameStep_FailureWins_Once()
        {var s=Session(Level(cols:1,health:1,enemy:new EnemyDefinition("e",100,100,1,1,3)));int results=0;s.Completed+=r=>results++;s.StartWave();Advance(s,1);Assert.That(s.BaseHealth,Is.LessThanOrEqualTo(0));Assert.That(s.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(s.Escapes,Is.EqualTo(1));Assert.That(results,Is.EqualTo(1));Assert.That(s.StartWave(),Is.False);}
        [Test] public void ExitAndRestartGeneration_DoNotPublishVictoryOrAcceptOldCommands()
        {var old=Session();int results=0;old.Completed+=r=>results++;var key=Put(old,1,0).Entity;old.StartWave();old.Advance(.02f);old.Dispose();old.Dispose();Assert.That(results,Is.Zero);var next=Session();Assert.That(next.Generation,Is.Not.EqualTo(old.Generation));Assert.That(next.Place(new PlacementCommand(old.Generation,2,"s",new Cell(0,0))).Rejection,Is.EqualTo(PlacementRejection.SessionExpired));Assert.That(next.RemoveUnit(key),Is.False);Assert.That(next.UnitCount,Is.Zero);}
        [Test] public void CreditIsSessionScoped_Deduplicated_AndCapped()
        {var s=Session();Assert.That(s.Credit(s.Generation,1,int.MaxValue,"test"));Assert.That(s.Resource,Is.EqualTo(9999));Assert.That(s.Credit(s.Generation,1,1,"test"),Is.False);Assert.That(s.Credit(s.Generation+1,2,1,"test"),Is.False);}
        [Test] public void ResultObserverCanDispose_AfterSimulationIterationEnds()
        {var s=Session();Put(s);s.Completed+=r=>s.Dispose();s.StartWave();Assert.DoesNotThrow(()=>Advance(s,1));Assert.That(s.Phase,Is.EqualTo(BattlePhase.Disposed));Assert.That(s.UnitCount,Is.Zero);}
        [Test] public void SimultaneousLargeBaseDamageCannotOverflowToVictory()
        {var s=Session(Level(cols:1,health:1,enemy:new EnemyDefinition("e",100,100,1,1,int.MaxValue),waves:new[]{new WaveDefinition("w",new SpawnEntry("e",0,0),new SpawnEntry("e",1,0))}));s.StartWave();s.Advance(.1f);Assert.That(s.Phase,Is.EqualTo(BattlePhase.Lost));Assert.That(s.BaseHealth,Is.LessThanOrEqualTo(0));Assert.That(s.Escapes,Is.EqualTo(2));}
        [Test] public void SharedCommandAllocationContinuesAfterExternalSubmission()
        {var s=Session();Assert.That(Put(s,0,0,100).Success);long next=s.NextCommandId();Assert.That(next,Is.EqualTo(101));Assert.That(s.Place(new PlacementCommand(s.Generation,next,"s",new Cell(0,1))).Success);Assert.That(s.UnitCount,Is.EqualTo(2));}
        [Test] public void CardAvailabilityIncludesLimitAndPhase()
        {var s=Session(Level(unit:new UnitDefinition("s","S",0,100,cooldown:0,limit:1)));Put(s);Assert.That(s.CardRejection("s"),Is.EqualTo(PlacementRejection.LimitReached));s.SetPaused(true);Assert.That(s.CardRejection("s"),Is.EqualTo(PlacementRejection.NotInteractive));}
        [Test] public void LargeExactRefundDoesNotOverflowThroughFloatRounding()
        {
            var level=Level();level.Configure("large",1,1,Vector2.one,int.MaxValue,1,
                new[]{new UnitDefinition("s","S",int.MaxValue,1,false,cooldown:0)},new[]{new EnemyDefinition("e")},Array.Empty<WaveDefinition>(),resourceCap:int.MaxValue,refund:1);
            var s=Session(level);var placed=Put(s);Assert.That(placed.Success);Assert.That(s.Resource,Is.Zero);Assert.That(s.RemoveUnit(placed.Entity));Assert.That(s.Resource,Is.EqualTo(int.MaxValue));
        }
    }
}
