using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void InstallPowers() => PlayerPowerTestAssets.Install(_config, item=>_objects.Add(item));
        private EntityPowerRuntime Power(PlayerController player, bool skill=false)
        {
            EntityPowerRuntime result;
            Assert.That(skill ? player.Context.Skills.TryGet("test.skill",out result)
                : player.Context.Abilities.TryGet("test.air",out result),Is.True);
            return result;
        }
        [Test] public void PowersSurviveStateTransitions_ContinuousSupportDoesNotRefillSpentUses()
        {
            InstallPowers();var player=Character();Land(player);
            var ability=Power(player);var skill=Power(player,true);
            ability.TryCommit(ability.CreateAttempt());skill.TryCommit(skill.CreateAttempt());
            var progress=player.Context.PowerProgress;
            Keys(Key.D);Decide(player);
            Assert.That(player.CurrentStateId,Is.EqualTo("move"));
            Keys();Decide(player);
            Assert.That(player.CurrentStateId,Is.EqualTo("idle"));
            Assert.That(Power(player),Is.SameAs(ability));Assert.That(player.Context.PowerProgress,Is.SameAs(progress));
            Assert.That(ability.RemainingUses,Is.EqualTo(1));
            Assert.That(skill.RemainingCooldown,Is.EqualTo(10-2*Step).Within(.00001));
        }
        [Test] public void BufferedLandingRefillsBeforeDirectJump_UpwardNearFloorDoesNotRefill()
        {
            InstallPowers();var player=Character(new Vector2(0,1.05f),new Vector2(0,-2));
            var ability=Power(player);ability.TryCommit(ability.CreateAttempt());ability.TryCommit(ability.CreateAttempt());
            Assert.That(ability.RemainingUses,Is.Zero);
            Keys(Key.Space);Decide(player);
            Assert.That(player.CurrentStateId,Is.EqualTo("fall"));
            for(int i=0;i<5;i++)
            {
                _scene.GetPhysicsScene2D().Simulate(Step);Decide(player);
                if(player.CurrentStateId!="jump")continue;
                Assert.That(ability.RemainingUses,Is.EqualTo(2));
                Assert.That(player.Context.JumpWindow.CanJump,Is.False);
                ability.TryCommit(ability.CreateAttempt());
                player.FixedTick(Step); // 未推进物理，Cast 仍在地面范围内。
                Assert.That(player.Context.Sensors.IsGrounded,Is.True);
                Assert.That(player.Context.JumpWindow.HasGroundSupport,Is.False);
                Assert.That(ability.RemainingUses,Is.EqualTo(1));return;
            }
            Assert.Fail("未在缓冲期间直接起跳");
        }
        [Test] public void ResourceTimeAdvancesOnlyInRenderDriver_NotPhysicsOrDiagnostics()
        {
            InstallPowers();var player=Character(new Vector2(0,5));var skill=Power(player,true);
            skill.TryCommit(skill.CreateAttempt());
            for(int i=0;i<20;i++){player.FixedTick(.013f);skill.Query();}
            Assert.That(skill.RemainingCooldown,Is.EqualTo(10));
            player.Tick(.37f);Assert.That(skill.RemainingCooldown,Is.EqualTo(9.63f).Within(.00001));
            player.Tick(0);Assert.That(skill.RemainingCooldown,Is.EqualTo(9.63f).Within(.00001));
        }
        [Test] public void CharacterResourcesAreIsolated_StoppedResourcesCannotReachNewCharacter()
        {
            InstallPowers();var first=Character(new Vector2(-3,5));var second=Character(new Vector2(3,5));
            var a=Power(first);var b=Power(second);var attempt=a.CreateAttempt();
            Assert.That(b.TryCommit(attempt),Is.EqualTo(EntityUseStatus.InvalidAttempt));
            a.TryCommit(attempt);a.SetOpen(false);
            Assert.That(b.IsOpen,Is.True);Assert.That(b.RemainingUses,Is.EqualTo(2));
            var progress=first.Context.PowerProgress;first.Stop();
            Assert.That(progress.IsAbilityUnlocked("test.air"),Is.False);
            Assert.That(a.Query().HasFlag(EntityUseStatus.Disposed),Is.True);
            var third=Character(new Vector2(7,5));
            Assert.That(Power(third).TryCommit(attempt),Is.EqualTo(EntityUseStatus.InvalidAttempt));
            Assert.That(Power(third).IsOpen,Is.True);
        }
        [Test] public void GroundSupportFactUsesEdgesAndDoesNotInventAirResources()
        {
            var window=new PlayerJumpWindow(.1f);
            Assert.That(window.Observe(true,0,Step),Is.True);
            for(int i=0;i<10;i++)Assert.That(window.Observe(true,0,Step),Is.False);
            window.Consume();Assert.That(window.Observe(true,10,Step),Is.False);
            Assert.That(window.CanJump,Is.False);
            Assert.That(window.Observe(false,-1,Step),Is.False);
            Assert.That(window.Observe(true,-1,Step),Is.True);
        }
    }
}
