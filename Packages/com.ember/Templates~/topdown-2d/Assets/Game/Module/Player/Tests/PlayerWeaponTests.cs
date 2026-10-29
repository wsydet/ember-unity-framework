using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Entity;
using Game.Player.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerLifecycleTests
    {
        [UnityTest] public IEnumerator FocusCanPreemptWeaponControlWithoutFaultAndWeaponCannotPreemptForeignOwner()=>ActionPreemption().ToCoroutine();
        private async UniTask ActionPreemption()
        {
            _scheme=Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerScheme>("Config/Player/FocusExample/PlayerScheme")));_player.SetSchemeForNextSession(_scheme);
            var player=await Running();var weapon=player.Context.Weapons.Current;
            using(player.Context.AcquireActionControl(this))
            {
                using(_player.RequestAction(player,PlayerActionIntent.Attack)){await Steps(1);}
                Assert.AreEqual("ready",weapon.CurrentState);Assert.AreEqual(0,weapon.Context.AttackInstance);
            }
            using(_player.RequestAction(player,PlayerActionIntent.Attack)){await Steps(1);}
            Assert.AreEqual("windup",weapon.CurrentState);Press(_keyboard.fKey);InputTick();await Steps(1);
            Assert.IsTrue(_player.HasSession);Assert.AreEqual("focus",player.CurrentState);Assert.AreEqual("ready",weapon.CurrentState);
            Assert.IsFalse(_player.TryEquipWeapon("weapon.training-pistol"));
        }
        private void WeaponPair()
        {EnableCombat(maximumUses:4);_scheme.ConfigureWeapons(_scheme.Weapons[0],TestWeapon("test.pistol",WeaponHitMode.Ray,4));}
        [UnityTest] public IEnumerator WeaponFireKeepsPlayerMovingAndUsesOnlyEquippedWeaponResources()=>MovingFire().ToCoroutine();
        private async UniTask MovingFire()
        {
            WeaponPair();Target(new Vector2(4,0));var player=await Running();Assert.IsTrue(_player.TryEquipWeapon("test.pistol"));Physics2D.SyncTransforms();
            var sword=player.Context.Weapons.Weapons[0];var pistol=player.Context.Weapons.Current;
            var from=player.Body.position;Sticks(move:Vector2.up,aim:Vector2.right);InputTick();Press(_keyboard.jKey);InputTick();await Steps(8);
            Assert.AreEqual("move",player.CurrentState);Assert.That(player.Body.position.y,Is.GreaterThan(from.y+.1f));
            Assert.AreEqual(6,_player.CaptureCombatActors().Single().Health);Assert.AreEqual(3,pistol.Context.Powers.Capture().GetSkill(0).RemainingUses);
            Assert.AreEqual(4,sword.Context.Powers.Capture().GetSkill(0).RemainingUses);Assert.IsFalse(player.Context.Powers.Skills.TryGet("fire",out _));
            Assert.IsFalse(player.Context.Scheme.Graph.Nodes.Any(n=>n.Id=="attack"));Assert.AreNotEqual(player.CaptureFsmObservation().InstanceId,pistol.CaptureObservation().InstanceId);
        }
        [UnityTest] public IEnumerator RangedWeaponStopsAtWallAndNearestEnemyAndDeduplicatesHurtboxes()=>RayOcclusion().ToCoroutine();
        private async UniTask RayOcclusion()
        {
            WeaponPair();var target=Target(new Vector2(4,0));var player=await Running();_player.TryEquipWeapon("test.pistol");
            var wall=Keep(new GameObject("Ray wall"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,_scene);wall.transform.position=new Vector2(2,0);wall.AddComponent<BoxCollider2D>();Physics2D.SyncTransforms();
            Press(_keyboard.jKey);InputTick();await Steps(8);Assert.AreEqual(8,_player.CaptureCombatActors().Single().Health);Release(_keyboard.jKey);InputTick();await Steps(14);
            UnityEngine.Object.DestroyImmediate(wall);Physics2D.SyncTransforms();Press(_keyboard.jKey);InputTick();await Steps(8);Assert.AreEqual(6,_player.CaptureCombatActors().Single().Health);
            Assert.That(player.Context.Weapons.Current.TracerDistance,Is.LessThan(4));
        }
        [UnityTest] public IEnumerator SwitchingWeaponsInterruptsOnlyWeaponAndDoesNotLeakMotionOrResumeOldHit()=>SwitchOwnership().ToCoroutine();
        private async UniTask SwitchOwnership()
        {
            WeaponPair();Target(Vector2.right);var player=await Running();var sword=player.Context.Weapons.Current;
            Press(_keyboard.jKey);InputTick();await Steps(1);Assert.AreEqual("windup",sword.CurrentState);Assert.AreEqual("idle",player.CurrentState);
            Assert.IsTrue(_player.TryEquipWeapon("test.pistol"));Assert.AreEqual("ready",sword.CurrentState);Sticks(move:Vector2.up);InputTick();await Steps(8);
            Assert.AreEqual("move",player.CurrentState);Assert.AreEqual(8,_player.CaptureCombatActors().Single().Health);
            Assert.IsTrue(_player.TryEquipWeapon("test.sword"));await Steps(8);Assert.AreEqual("ready",sword.CurrentState);Assert.AreEqual(8,_player.CaptureCombatActors().Single().Health);
            using(player.Context.AcquireActionControl(this)){};
        }
        [UnityTest] public IEnumerator WeaponPauseHitDeathAndRetiredSessionReleaseOnlyOwnedControls()=>WeaponLifetime().ToCoroutine();
        private async UniTask WeaponLifetime()
        {
            WeaponPair();var player=await Running();Press(_keyboard.jKey);InputTick();await Steps(1);var sword=player.Context.Weapons.Current;
            using(_player.Pause(this)){var path=sword.CaptureObservation().ActivePath;await UniTask.Yield();Assert.AreEqual(path,sword.CaptureObservation().ActivePath);Assert.IsFalse(_player.TryEquipWeapon("test.pistol"));}
            player.Context.HitReceiver.Receive(new EntityHit("physical",1,Vector2.up*5,30,.2f));await Steps(1);
            Assert.AreEqual("hit",player.CurrentState);Assert.AreEqual("ready",sword.CurrentState);Assert.That(player.Context.Velocity.y,Is.GreaterThan(0));
            using(player.Context.AcquireActionControl(this)){};
            player.Context.Effects.RemoveAll("damage-protection");player.Context.HitReceiver.Receive(new EntityHit("physical",100,Vector2.zero,0,0));await Steps(1);
            Assert.AreEqual("dead",player.CurrentState);Assert.AreEqual("ready",sword.CurrentState);_player.StopSession();Assert.AreEqual(0,player.Context?.Weapons.Weapons.Count??0);
        }
        [UnityTest] public IEnumerator WeaponSaveRestoresEquippedIdAndEveryWeaponsCooldownAndRejectsDifferentLoadout()=>WeaponProgress().ToCoroutine();
        private async UniTask WeaponProgress()
        {
            WeaponPair();ProgressWorld();var player=await Running();var sword=player.Context.Weapons.Current;
            sword.Context.Powers.Skills.TryGet("fire",out var fire);Assert.AreEqual(EntityUseStatus.Available,fire.TryCommit(fire.CreateAttempt()));
            _player.TryEquipWeapon("test.pistol");Assert.IsTrue(_player.TryCaptureSnapshot(out var snapshot,out var reason),reason);
            Assert.AreEqual("test.pistol",snapshot.Weapons.EquippedId);Assert.AreEqual(3,snapshot.Weapons.Get(0).Powers.GetSkill(0).RemainingUses);
            var codec=new TopdownSnapshotCodec();var encoded=codec.Encode(snapshot);var decoded=codec.Decode(encoded.Payload,TopdownSnapshot.DATA_VERSION);Assert.IsNotNull(decoded.Progress,decoded.Error);
            var next=await _player.PrepareAsync(player,_input,_data,entry:new TopdownEntryRequest(decoded.Progress,TopdownRecoveryReason.Load),replacement:true);_player.StartSession(next);
            Assert.AreEqual("test.pistol",player.Context.Weapons.Current.Spec.Id);Assert.AreEqual(3,player.Context.Weapons.Weapons[0].Context.Powers.Capture().GetSkill(0).RemainingUses);
            Assert.That(player.Context.Weapons.Weapons[0].Context.Powers.Capture().GetSkill(0).RemainingCooldown,Is.EqualTo(snapshot.Weapons.Get(0).Powers.GetSkill(0).RemainingCooldown).Within(.001f));
            var bad=new WeaponLoadoutProgress("other",new[]{new WeaponProgress("other",snapshot.Weapons.Get(0).Powers)});
            var incompatible=new TopdownSnapshot(snapshot.Scene,snapshot.LevelId,snapshot.SpawnId,snapshot.SchemeId,snapshot.Position,snapshot.Facing,snapshot.Checkpoint,true,snapshot.Health,snapshot.Powers,
                Array.Empty<EntityTimedEffectSnapshot>(),Array.Empty<TopdownActorRecord>(),new[]{snapshot.GetInteraction(0)},bad);
            var current=player.Context;var failed=false;try{await _player.PrepareAsync(player,_input,_data,entry:new TopdownEntryRequest(incompatible,TopdownRecoveryReason.Load),replacement:true);}catch(InvalidOperationException){failed=true;}
            Assert.IsTrue(failed);Assert.AreSame(current,player.Context);Assert.IsTrue(player.IsRunning);
        }
        [UnityTest] public IEnumerator WeaponSourceChangesDoNotMutateRunningHostAndHostsDoNotShareResources()=>WeaponFreeze().ToCoroutine();
        private async UniTask WeaponFreeze()
        {
            WeaponPair();var source=_scheme.Weapons[0];var player=await Running();var old=player.Context.Weapons.Current;
            Assert.AreNotSame(source.Graph,old.Spec.Graph);Field(source,"_damage",99f);Assert.AreEqual(2,old.Spec.Damage);
            var next=await _player.PrepareAsync(Character(),_input,_data);old.Context.Powers.Skills.TryGet("fire",out var skill);skill.TryCommit(skill.CreateAttempt());
            var other=next.Controller;Assert.IsFalse(other.IsRunning);next.Dispose();Assert.IsTrue(player.IsRunning);Assert.AreEqual(3,old.Context.Powers.Capture().GetSkill(0).RemainingUses);
        }
    }
}
