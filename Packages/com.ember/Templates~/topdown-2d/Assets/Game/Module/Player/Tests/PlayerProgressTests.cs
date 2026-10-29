using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerLifecycleTests
    {
        [UnityTest] public IEnumerator ProgressReadLeaseSerializesExitAndOldReleaseCannotClearNewLease()=>ReadOwnership().ToCoroutine();
        private async UniTask ReadOwnership()
        {
            ProgressWorld();await Running();var old=_player.BeginProgressRead(this);
            Assert.IsTrue(_player.ProgressBusy&&_player.IsPaused);Assert.IsFalse(_player.CanLeaveGameplay);
            Assert.Throws<InvalidOperationException>(()=>_player.BeginProgressRead(_player));
            _player.StopSession();Assert.IsFalse(_player.ProgressBusy||_player.IsPaused);await Running();
            using(_player.BeginProgressRead(this)){old.Dispose();Assert.IsTrue(_player.ProgressBusy&&_player.IsPaused);}
            Assert.IsTrue(_player.CanLeaveGameplay);Assert.IsFalse(_player.IsPaused);
        }
        private void ProgressWorld()
        {
            var root=Keep(new GameObject("Progress identity"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,_scene);
            root.AddComponent<TopdownLevelIdentity>();
            var item=Keep(new GameObject("Persistent checkpoint"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item,_scene);item.AddComponent<TopdownInteraction>();
        }
        [UnityTest] public IEnumerator SaveFreezesPlayerAndWorldAndReplacementOwnsIndependentResources()=>ProgressRoundTrip().ToCoroutine();
        private async UniTask ProgressRoundTrip()
        {
            EnableCombat();ProgressWorld();Target(new Vector2(4,0));var controller=await Running();controller.Context.Health.Restore(4);
            controller.Context.Effects.ApplyCaptured(new EntityEffectSpec("test.protection",EntityEffectKind.Buff,EntityEffectLifetime.Timed,2,
                new EntityEffectRules(new[]{"physical"},false,0),new EntityEffectAppearance(true,10,.35f,8)),"test.protection");
            Press(_keyboard.eKey);InputTick();Assert.IsTrue(_player.TryCaptureSnapshot(out var before,out var error),error);
            Assert.IsTrue(before.GetInteraction(0).Activated);Assert.AreEqual(1,before.ActorCount);
            var save=_player.SaveProgressAsync(0);controller.Body.position=new Vector2(5,3);controller.Context.Health.Restore(1);
            Assert.IsTrue(await save);var entry=await TopdownEntryRequest.ReadAsync(_data,0,TopdownRecoveryReason.Load);
            Assert.AreEqual(before.Position,entry.Snapshot.Position);Assert.AreEqual(4,entry.Snapshot.Health);
            Assert.AreEqual(1,entry.Snapshot.EffectCount);Assert.IsTrue(entry.Snapshot.GetInteraction(0).Activated);
            var oldContext=controller.Context;
            var discarded=await _player.PrepareAsync(controller,_input,_data,entry:entry,replacement:true);
            Assert.AreSame(oldContext,controller.Context);Assert.AreEqual(new Vector2(5,3),controller.Body.position);
            discarded.Dispose();Assert.AreSame(oldContext,controller.Context);Assert.IsTrue(controller.IsRunning);
            var restored=await _player.PrepareAsync(controller,_input,_data,entry:entry,replacement:true);
            _player.StartSession(restored);Assert.AreNotSame(oldContext,controller.Context);
            Assert.AreEqual(before.Position,controller.Body.position);Assert.AreEqual(4,controller.Context.Health.Current);
            Assert.AreEqual(1,controller.Context.Effects.Count);Assert.IsTrue(_player.TryCaptureSnapshot(out var after,out error),error);
            Assert.IsTrue(after.GetInteraction(0).Activated);discarded.Dispose();Assert.IsTrue(controller.IsRunning);
        }
        [UnityTest] public IEnumerator IncompatibleCandidateAndInvalidSlotPreserveCurrentSessionAndGoodSave()=>ProgressFailures().ToCoroutine();
        private async UniTask ProgressFailures()
        {
            EnableCombat();ProgressWorld();var controller=await Running();Assert.IsTrue(await _player.SaveProgressAsync(0));
            Assert.IsTrue(_player.TryCaptureSnapshot(out var original,out _));
            var bad=new TopdownSnapshot(original.Scene,original.LevelId,original.SpawnId,"unrelated.scheme",original.Position,original.Facing,original.Checkpoint,
                original.CombatInstalled,original.Health,original.Powers,Array.Empty<EntityTimedEffectSnapshot>(),Array.Empty<TopdownActorRecord>(),new[]{original.GetInteraction(0)});
            var old=controller.Context;var failed=false;
            try{await _player.PrepareAsync(controller,_input,_data,entry:new TopdownEntryRequest(bad,TopdownRecoveryReason.Load),replacement:true);}catch(InvalidOperationException){failed=true;}
            Assert.IsTrue(failed);Assert.AreSame(old,controller.Context);Assert.IsTrue(controller.IsRunning&&controller.Context.ControlsAvailable);
            Assert.IsFalse(await _player.SaveProgressAsync(99));var valid=await TopdownEntryRequest.ReadAsync(_data,0,TopdownRecoveryReason.Load);Assert.AreEqual(original.Health,valid.Snapshot.Health);
        }
        [UnityTest] public IEnumerator UnstableActionsRejectCaptureAndRetryHasExplicitCheckpointWorld()=>StableBoundary().ToCoroutine();
        private async UniTask StableBoundary()
        {
            EnableCombat();ProgressWorld();var controller=await Running();var retry=_player.CreateRetryRequest();
            Press(_keyboard.jKey);InputTick();await Steps(1);Assert.AreEqual("idle",controller.CurrentState);Assert.AreEqual("windup",controller.Context.Weapons.Current.CurrentState);
            Assert.IsFalse(_player.TryCaptureSnapshot(out _,out var reason));StringAssert.Contains("武器",reason);
            Assert.IsFalse(await _player.SaveProgressAsync(0));
            Assert.AreEqual(TopdownRecoveryReason.Retry,retry.Reason);Assert.AreEqual(6,retry.Snapshot.Health);Assert.IsFalse(retry.Snapshot.GetInteraction(0).Activated);
        }
        [UnityTest] public IEnumerator RepeatedFreshAttacksAfterFullDurationEachCommitOnce()=>RepeatedCombat().ToCoroutine();
        [UnityTest] public IEnumerator DrainRetainsThePausedRunningSessionUntilAcceptedWritesComplete()=>DrainLifetime().ToCoroutine();
        private async UniTask DrainLifetime()
        {
            ProgressWorld();var controller=await Running();var saving=_player.SaveProgressAsync(0);
            using(_player.Pause(this))
            {
                var ticket=_data.BeginDrain();
                try
                {
                    ((IEmberUpdate)_player).Update();Assert.IsTrue(_player.HasSession);Assert.IsTrue(controller.IsRunning);
                    Assert.IsTrue(await saving);var result=await _data.DrainAsync(ticket);Assert.AreEqual(DrainStatus.Drained,result.Status);
                    Assert.IsTrue(_player.HasSession);
                }
                finally{_data.ResumeRequests(ticket);}
            }
        }
        private async UniTask RepeatedCombat()
        {
            EnableCombat();var controller=await Running();controller.Context.Weapons.Current.Context.Powers.Skills.TryGet("fire",out var skill);
            for(var i=0;i<2;i++)
            {
                Press(_keyboard.jKey);InputTick();await Steps(1);Assert.AreEqual("windup",controller.Context.Weapons.Current.CurrentState);Assert.AreEqual("idle",controller.CurrentState);Assert.AreEqual(1-i,skill.RemainingUses);
                Release(_keyboard.jKey);InputTick();await Steps(Mathf.CeilToInt(.4f/Time.fixedDeltaTime)+2);Assert.AreEqual("idle",controller.CurrentState);
            }
        }
    }

    public sealed class TopdownCodecTests
    {
        private TopdownSnapshot Sample()
        {
            var effect=new EntityTimedEffectSnapshot("protection","hit",.25f,new EntityEffectRules(new[]{"physical"},true,2),new EntityEffectAppearance(true,5,.4f,8));
            return new TopdownSnapshot("Assets/Game/Scenes/GameplayScene.unity","starter","start","test",new Vector2(2,3),Vector2.up,Vector2.zero,true,4,
                new EntityPowerSnapshot(Array.Empty<EntityPowerRecord>(),new[]{new EntityPowerRecord("dash",true,2,.3f)}),new[]{effect},
                new[]{new TopdownActorRecord("enemy",0,new Vector2(4,5))},new[]{new TopdownInteractionRecord("switch",true)},
                new Weapons.WeaponLoadoutProgress("pistol",new[]{new Weapons.WeaponProgress("sword",new EntityPowerSnapshot(Array.Empty<EntityPowerRecord>(),new[]{new EntityPowerRecord("fire",true,3,.2f)})),
                    new Weapons.WeaponProgress("pistol",new EntityPowerSnapshot(Array.Empty<EntityPowerRecord>(),new[]{new EntityPowerRecord("fire",true,4,.1f)}))}));
        }
        [Test] public void CodecRetainsCompletePlayerAndWorldAndRejectsTruncationTrailingAndVersions()
        {
            var codec=new TopdownSnapshotCodec();var encoded=codec.Encode(Sample());var result=codec.Decode(encoded.Payload,TopdownSnapshot.DATA_VERSION);
            Assert.AreEqual(DecodeStatus.Decoded,result.Status);var copy=result.Progress;
            Assert.AreEqual(new Vector2(2,3),copy.Position);Assert.AreEqual(Vector2.up,copy.Facing);Assert.AreEqual(4,copy.Health);
            Assert.AreEqual(2,copy.Powers.GetSkill(0).RemainingUses);Assert.AreEqual(.3f,copy.Powers.GetSkill(0).RemainingCooldown);
            Assert.AreEqual(8,copy.GetEffect(0).Appearance.BlinkHz);Assert.IsTrue(copy.GetEffect(0).Rules.PreventsStagger);
            Assert.AreEqual(0,copy.GetActor(0).Health);Assert.IsTrue(copy.GetInteraction(0).Activated);
            Assert.AreEqual("pistol",copy.Weapons.EquippedId);Assert.AreEqual(2,copy.Weapons.Count);Assert.AreEqual(3,copy.Weapons.Get(0).Powers.GetSkill(0).RemainingUses);Assert.AreEqual(.1f,copy.Weapons.Get(1).Powers.GetSkill(0).RemainingCooldown);
            Assert.AreEqual(DecodeStatus.DataVersionUnsupported,codec.Decode(encoded.Payload,1).Status);
            Assert.AreEqual(DecodeStatus.DecodeFailed,codec.Decode(encoded.Payload.Take(encoded.Payload.Length-1).ToArray(),TopdownSnapshot.DATA_VERSION).Status);
            Assert.AreEqual(DecodeStatus.DecodeFailed,codec.Decode(encoded.Payload.Concat(new byte[]{0}).ToArray(),TopdownSnapshot.DATA_VERSION).Status);
        }
        [Test] public void SnapshotRejectsInvalidFacingAndDuplicateWorldIdentity()
        {
            var s=Sample();Assert.Throws<ArgumentException>(()=>new TopdownSnapshot(s.Scene,s.LevelId,s.SpawnId,s.SchemeId,s.Position,Vector2.zero,s.Checkpoint,true,4,
                s.Powers,Array.Empty<EntityTimedEffectSnapshot>(),Array.Empty<TopdownActorRecord>(),Array.Empty<TopdownInteractionRecord>()));
            Assert.Throws<ArgumentException>(()=>new TopdownSnapshot(s.Scene,s.LevelId,s.SpawnId,s.SchemeId,s.Position,s.Facing,s.Checkpoint,true,4,s.Powers,
                Array.Empty<EntityTimedEffectSnapshot>(),new[]{s.GetActor(0),s.GetActor(0)},Array.Empty<TopdownInteractionRecord>()));
        }
        [Test] public void WeaponProgressRejectsDuplicatesMissingEquippedAndExcessSlots()
        {
            var s=Sample().Weapons;var one=s.Get(0);
            Assert.Throws<ArgumentException>(()=>new Weapons.WeaponLoadoutProgress(one.Id,new[]{one,one}));
            Assert.Throws<ArgumentException>(()=>new Weapons.WeaponLoadoutProgress("missing",new[]{one}));
            Assert.Throws<ArgumentException>(()=>new Weapons.WeaponLoadoutProgress("missing",Array.Empty<Weapons.WeaponProgress>()));
            Assert.Throws<ArgumentException>(()=>new Weapons.WeaponLoadoutProgress(one.Id,Enumerable.Repeat(one,17).ToArray()));
        }
    }
}
