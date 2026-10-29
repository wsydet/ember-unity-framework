using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerLifecycleTests
    {
        [UnityTest] public IEnumerator ReactionUsesIncomingPlanarBrakingAndReleasesVelocityAtStunEnd()=>ReactionBraking().ToCoroutine();
        private async UniTask ReactionBraking()
        {
            EnableCombat();var c=await Running();c.Context.HitReceiver.Receive(new EntityHit("physical",0,Vector2.up*10,100,.2f));
            await Steps(1);Assert.AreEqual("hit",c.CurrentState);var speed=c.Context.Velocity.magnitude;
            await Steps(1);Assert.That(c.Context.Velocity.magnitude,Is.EqualTo(Mathf.Max(0,speed-100*Time.fixedDeltaTime)).Within(.001f));
            await Steps(Mathf.CeilToInt(.3f/Time.fixedDeltaTime)+1);Assert.AreEqual("idle",c.CurrentState);Assert.AreEqual(Vector2.zero,c.Context.Velocity);
        }
        [UnityTest] public IEnumerator RetiredReceiverCannotDamageEitherOldOrNewSession()=>RetiredReceiver().ToCoroutine();
        private async UniTask RetiredReceiver()
        {
            EnableCombat();var controller=await Running();var old=controller.Context;_player.StopSession();
            Assert.IsFalse(old.HitReceiver.Receive(new EntityHit("physical",2,Vector2.zero,30,0)).Accepted);Assert.AreEqual(6,old.Health.Current);
            var next=await _player.PrepareAsync(controller,_input,_data);_player.StartSession(next);
            Assert.IsFalse(old.HitReceiver.Receive(new EntityHit("physical",2,Vector2.zero,30,0)).Accepted);Assert.AreEqual(6,controller.Context.Health.Current);
        }
        private sealed class TestCombatRule:IPlayerCombatRules
        {
            public int Calls;
            public EntityHit? Filter(PlayerCombatTarget target,in EntityHit hit)
            {Calls++;return target.Id=="player"?null:new EntityHit(hit.Category,hit.Damage*.5f,hit.Velocity,hit.Braking,hit.StunSeconds,hit.Priority);}
        }
        private sealed class TestInteractionResolver:IPlayerInteractionResolver
        {public int Calls;public bool TryInteract(in PlayerInteractionIntent intent){Calls++;return true;}}
        [UnityTest] public IEnumerator OwnedRequestsCancelOnlyThemselvesAndDenyDiscardsPendingIntent()=>OwnedRequests().ToCoroutine();
        private async UniTask OwnedRequests()
        {
            EnableCombat();var resolver=new TestInteractionResolver();_player.SetInteractionResolverForNextSession(resolver);var controller=await Running();
            var one=_player.RequestAction(this,PlayerActionIntent.Attack);var two=_player.RequestAction(controller,PlayerActionIntent.Attack);one.Dispose();await Steps(1);
            Assert.IsFalse(one.WasConsidered);Assert.IsTrue(two.WasConsidered);Assert.AreEqual("windup",controller.Context.Weapons.Current.CurrentState);Assert.AreEqual("idle",controller.CurrentState);two.Dispose();
            await Steps(Mathf.CeilToInt(.4f/Time.fixedDeltaTime)+2);
            var dash=_player.RequestAction(this,PlayerActionIntent.Dash);using(_player.Pause(this)){}await Steps(1);
            Assert.IsFalse(dash.WasConsidered||dash.IsPending);Assert.AreEqual("idle",controller.CurrentState);
            using(_player.RequestAction(this,PlayerActionIntent.Interact)){await Steps(2);}
            Assert.AreEqual(1,resolver.Calls);
        }
        [UnityTest] public IEnumerator ProjectRuleRunsBeforeHealthMutationAndReturnsFinalHitValue()=>ProjectHitRule().ToCoroutine();
        private async UniTask ProjectHitRule()
        {
            EnableCombat();var rule=new TestCombatRule();_player.SetCombatRulesForNextSession(rule);Target(Vector2.right);var controller=await Running();Physics2D.SyncTransforms();
            var denied=controller.Context.HitReceiver.Receive(new EntityHit("physical",5,Vector2.up,30,.2f));Assert.IsFalse(denied.Accepted);Assert.AreEqual(6,controller.Context.Health.Current);
            using(_player.RequestAction(this,PlayerActionIntent.Attack)){await Steps(8);}
            Assert.AreEqual(7,_player.CaptureCombatActors().Single().Health);Assert.AreEqual(2,rule.Calls);
        }
        [UnityTest] public IEnumerator FocusExampleReusesBaseStatesAndReleasesOwnershipWhenInterrupted()=>FocusExtension().ToCoroutine();
        private async UniTask FocusExtension()
        {
            ProgressWorld();_scheme=Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerScheme>("Config/Player/FocusExample/PlayerScheme")));
            _player.SetSchemeForNextSession(_scheme);var controller=await Running();
            Assert.AreEqual(9,controller.Context.Scheme.Graph.Nodes.Count);
            Press(_keyboard.fKey);InputTick();await Steps(1);Assert.AreEqual("focus",controller.CurrentState);Assert.AreEqual(1,controller.Context.Effects.Count);
            Assert.IsFalse(_player.TryCaptureSnapshot(out _,out _));Assert.AreEqual(Vector2.zero,controller.Context.Velocity);
            controller.Context.HitReceiver.Receive(new EntityHit("physical",1,Vector2.up*3,30,.2f));await Steps(1);
            Assert.AreEqual("hit",controller.CurrentState);Assert.IsFalse(controller.Context.Effects.CaptureActive().Any(e=>e.EffectId=="focus-glow"));
            using(var lease=controller.Context.AcquireActionControl(this))lease.SetVelocity(Vector2.zero);
            Release(_keyboard.fKey);InputTick();
            for(var wait=0;wait<200&&controller.Context.Powers.Skills.Query("focus")!=EntityUseStatus.Available;wait++)await Steps(1);
            Assert.AreEqual(EntityUseStatus.Available,controller.Context.Powers.Skills.Query("focus"));
            Press(_keyboard.fKey);InputTick();await Steps(1);Assert.AreEqual("focus",controller.CurrentState);
            await Steps(Mathf.CeilToInt(.55f/Time.fixedDeltaTime)+1);Assert.AreEqual("idle",controller.CurrentState);
            Assert.IsFalse(controller.Context.Effects.CaptureActive().Any(e=>e.EffectId=="focus-glow"));
        }
    }
}
