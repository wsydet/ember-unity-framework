using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerLifecycleTests
    {
        private void Field(object target,string field,object value)
            =>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private void EnableCombat(bool invulnerable=false,bool shortWindow=false,int maximumUses=2)
        {
            var skills=new EntitySkillSlot[1];var names=new[]{"dash"};
            for(var i=0;i<1;i++)
            {
                var skill=Keep(ScriptableObject.CreateInstance<EntitySkillDefinition>());
                Field(skill,"_id",names[i]);Field(skill,"_defaultOpen",true);Field(skill,"_limitedUses",true);Field(skill,"_maximumUses",maximumUses);
                Field(skill,"_cooldownSeconds",.1f);skills[i]=new EntitySkillSlot(skill);
            }
            var powers=Keep(ScriptableObject.CreateInstance<PlayerPowersDefinition>());powers.Configure(Array.Empty<EntityAbilitySlot>(),skills);
            var settings=new PlayerCombatSettings();Field(settings,"_dashInvulnerable",invulnerable);
            _scheme.ConfigureCombat(true,powers,settings);
            _scheme.ConfigureWeapons(TestWeapon("test.sword",Weapons.WeaponHitMode.Melee,maximumUses,shortWindow));
        }
        private Weapons.WeaponDefinition TestWeapon(string id,Weapons.WeaponHitMode mode,int uses=2,bool shortWindow=false)
        {
            var skill=Keep(ScriptableObject.CreateInstance<EntitySkillDefinition>());Field(skill,"_id","fire");Field(skill,"_defaultOpen",true);
            Field(skill,"_limitedUses",true);Field(skill,"_maximumUses",uses);Field(skill,"_cooldownSeconds",.1f);
            var powers=Keep(ScriptableObject.CreateInstance<PlayerPowersDefinition>());powers.Configure(Array.Empty<EntityAbilitySlot>(),new[]{new EntitySkillSlot(skill)});
            var graph=Keep(ScriptableObject.CreateInstance<Ember.Core.Fsm.EmberFsmGraph>());var nodes=new System.Collections.Generic.List<Ember.Core.Fsm.EmberFsmNode>();var ids=new[]{"ready","windup","active","recovery"};
            for(var i=0;i<4;i++){var state=Keep(ScriptableObject.CreateInstance<Weapons.WeaponStateDefinition>());state.Configure((Weapons.WeaponStateKind)i);nodes.Add(new Ember.Core.Fsm.EmberFsmNode(ids[i],ids[i],state,null,null,new Ember.Core.Fsm.EmberFsmTargetBinding(ids[(i+1)%4],ids[(i+1)%4])));}
            graph.Configure("ready",nodes.ToArray());var weapon=Keep(ScriptableObject.CreateInstance<Weapons.WeaponDefinition>());
            weapon.Configure(id,id,graph,powers,mode,mode==Weapons.WeaponHitMode.Melee,mode==Weapons.WeaponHitMode.Melee,shortWindow?.001f:.06f,shortWindow?.001f:.08f,shortWindow?.01f:.21f);return weapon;
        }
        private TopdownCombatActor Target(Vector2 position,bool chase=false,int team=2)
        {
            var root=Keep(new GameObject("Combat Target"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,_scene);
            root.layer=2;root.transform.position=position;root.AddComponent<Entity2D>();
            var body=root.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
            root.AddComponent<BoxCollider2D>().size=new Vector2(.45f,.45f);
            var extra=new GameObject("Second hurtbox");extra.transform.SetParent(root.transform,false);extra.layer=2;extra.AddComponent<BoxCollider2D>().isTrigger=true;
            var actor=root.AddComponent<TopdownCombatActor>();Field(actor,"_stableId","test.target");Field(actor,"_chase",chase);Field(actor,"_team",team);
            return actor;
        }
        private void InputTick()
        {UnityEngine.InputSystem.InputSystem.Update();((IEmberUpdate)_player).Update();}
        private async UniTask Steps(int count)
        {
            for(var i=0;i<count;i++)
            {
                // 与正式驱动一致，在 Dynamic/Update 阶段采样输入，不能在 LastFixedUpdate 读 Dynamic 缓存。
                await UniTask.Yield(PlayerLoopTiming.Update);
                ((IEmberUpdate)_player).Update();((IEmberFixedUpdate)_player).FixedUpdate();
                await UniTask.WaitForFixedUpdate();
            }
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        [UnityTest] public IEnumerator EightDirectionsAndMultipleHurtboxesDeduplicatePerWindow()=>DirectionalHits().ToCoroutine();
        private async UniTask DirectionalHits()
        {
            EnableCombat();
            for(var i=0;i<8;i++)
            {
                var angle=i*Mathf.PI/4;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var target=Target(direction);var controller=await Running();Physics2D.SyncTransforms();
                Sticks(aim:direction);InputTick();Press(_keyboard.jKey);InputTick();await Steps(8);
                var view=_player.CaptureCombatActors().Single();Assert.That(view.Health,Is.EqualTo(6),$"direction={i}, state={controller.CurrentState}, facing={controller.Context.Facing}");
                Assert.That(Vector2.Dot(controller.Context.Facing,direction),Is.GreaterThan(.99f));
                _player.StopSession();UnityEngine.Object.DestroyImmediate(target.gameObject);UnityEngine.Object.DestroyImmediate(controller.gameObject);
                Release(_keyboard.jKey);InputTick();
            }
        }

        [UnityTest] public IEnumerator WindowCrossingCanHitEvenWhenWindowIsShorterThanPhysicsStep()=>ShortHit().ToCoroutine();
        private async UniTask ShortHit()
        {
            EnableCombat(shortWindow:true);Target(Vector2.right);await Running();Physics2D.SyncTransforms();
            Press(_keyboard.jKey);InputTick();await Steps(3);
            Assert.AreEqual(6,_player.CaptureCombatActors().Single().Health);
        }

        [UnityTest] public IEnumerator RejectedEntryDoesNotConsumeThenHitInterruptsCommittedAttack()=>PreflightAndInterrupt().ToCoroutine();
        private async UniTask PreflightAndInterrupt()
        {
            EnableCombat();var controller=await Running();controller.Context.Weapons.Current.Context.Powers.Skills.TryGet("fire",out var skill);skill.SetOpen(false);
            Press(_keyboard.jKey);InputTick();await Steps(1);Assert.AreEqual("idle",controller.CurrentState);Assert.AreEqual(2,skill.RemainingUses);
            Release(_keyboard.jKey);InputTick();skill.SetOpen(true);Press(_keyboard.jKey);InputTick();await Steps(1);
            Assert.AreEqual("idle",controller.CurrentState);Assert.AreEqual("windup",controller.Context.Weapons.Current.CurrentState);Assert.AreEqual(1,skill.RemainingUses);
            controller.Context.HitReceiver.Receive(new EntityHit("physical",1,Vector2.up*3,30,.2f));await Steps(1);
            Assert.AreEqual("hit",controller.CurrentState);Assert.AreEqual(5,controller.Context.Health.Current);Assert.AreEqual(1,skill.RemainingUses);
        }

        [UnityTest] public IEnumerator DashProtectionIsOwnedAndWallSweepDoesNotTunnel()=>DashProtection().ToCoroutine();
        private async UniTask DashProtection()
        {
            EnableCombat(invulnerable:true);var controller=await Running();
            var wall=Keep(new GameObject("Dash wall"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,_scene);
            wall.transform.position=Vector2.right*1.5f;wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,10);Physics2D.SyncTransforms();
            Press(_keyboard.leftShiftKey);InputTick();await Steps(1);Assert.AreEqual("dash",controller.CurrentState);
            var hit=controller.Context.HitReceiver.Receive(new EntityHit("physical",1,Vector2.left,30,.2f));
            Assert.IsFalse(hit.Accepted);Assert.AreEqual(6,controller.Context.Health.Current);
            await Steps(10);Assert.That(controller.Body.position.x,Is.LessThan(1));Assert.AreEqual(0,controller.Context.Effects.Count);
        }

        [UnityTest] public IEnumerator EnemyTelegraphsBeforeDamageAndLethalPlayerStopsInput()=>EnemyAndDeath().ToCoroutine();
        private async UniTask EnemyAndDeath()
        {
            EnableCombat();Target(Vector2.right,chase:true);var controller=await Running();Physics2D.SyncTransforms();
            await Steps(1);Assert.AreEqual("windup",_player.CaptureCombatActors().Single().Phase);await Steps(10);
            Assert.AreEqual(6,controller.Context.Health.Current);await Steps(18);Assert.That(controller.Context.Health.Current,Is.LessThan(6));
            controller.Context.Effects.RemoveAll("damage-protection");
            controller.Context.HitReceiver.Receive(new EntityHit("physical",100,Vector2.zero,30,0));await Steps(2);
            Assert.AreEqual("dead",controller.CurrentState);Assert.IsFalse(controller.Context.ControlsAvailable);
            Assert.IsFalse(controller.Context.HitReceiver.Receive(new EntityHit("physical",1,Vector2.zero,30,0)).Accepted);
        }

        [UnityTest] public IEnumerator FriendlyActorsDoNotTakeMeleeDamage()=>Friendly().ToCoroutine();
        private async UniTask Friendly()
        {
            EnableCombat();Target(Vector2.right,team:1);await Running();Physics2D.SyncTransforms();Press(_keyboard.jKey);InputTick();await Steps(8);
            Assert.AreEqual(8,_player.CaptureCombatActors().Single().Health);
        }
    }
}
