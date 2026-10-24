using System.Linq;
using System.Reflection;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void RuleRejectTarget(string source, string slot)
        {
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            var rejected = Keep(ScriptableObject.CreateInstance<RejectedJumpDefinition>());
            var nodes = _config.Graph.Nodes.Select(n => n.Id != source ? n : new EmberFsmNode(n.Id, n.DisplayName,
                n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.Select(b =>
                    new EmberFsmTargetBinding(b.Slot, b.Slot == slot ? "rule-rejected" : b.TargetId)).ToArray())).ToList();
            nodes.Add(new EmberFsmNode("rule-rejected", "Reject", rejected, _config.Graph.EntryId));
            graph.Configure(_config.Graph.EntryId, nodes.ToArray()); _config.Configure(graph, _config.Spawn);
        }
        [Test] public void RuleFormalGraphHasCompleteSitesWithoutChangingTransitionIdentity()
        {
            var graph = _config.Graph.Inspect(); Assert.That(graph.IsValid, Is.True);
            Assert.That(graph.Nodes.Count, Is.EqualTo(21)); Assert.That(graph.Nodes.Sum(n => n.Transitions.Count), Is.EqualTo(56));
            Assert.That(graph.Nodes.Sum(n => n.Transitions.Sum(t => t.Sites.Count)), Is.EqualTo(57));
            Assert.That(graph.Nodes.SelectMany(n => n.Transitions).All(t => t.Sites.Count > 0), Is.True);
            Assert.That(graph.Nodes.First(n => n.Id == "hard-land").Transitions.Select(t => t.Id), Is.EquivalentTo(new[] { "finished", "lost-support" }));
            Assert.That(PlayerRollingState.Ground.Sites.Select(s => s.Id), Is.EquivalentTo(new[] { "wall", "elapsed" }));
        }
        [Test] public void RuleExternalNormalLandingCannotBypassPendingJump()
        {
            LandingSettings(false); var player = Character(); Land(player);
            SetAirField(player.Context.Landing, "<Pending>k__BackingField", true);
            Keys(Key.Space); player.Tick(Step);
            Assert.That(player.Context.CanStartJump, Is.True);
            Assert.That(Fsm(player).GetActivation("basic-drive").Request(PlayerLocomotionState.Land), Is.EqualTo(EmberFsmResult.GuardRejected));
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            Assert.That(Fsm(player).LastOperation.Attempt.CommitStarted, Is.False);
        }
        [Test] public void RuleRejectedLedgeReleaseDoesNotConsumeOrBlockGrip()
        {
            InstallLedge(); RuleRejectTarget("ledge-hang", "fall"); var player = Hanging();
            var field = typeof(PlayerLedgeMotion).GetField("_blockedSurface", BindingFlags.NonPublic | BindingFlags.Instance);
            var before = field.GetValue(player.Context.Ledge);
            Keys(Key.S, Key.Space); player.Tick(Step);
            Assert.That(Fsm(player).GetActivation("ledge-hang").Request(PlayerLedgeHangState.Fall), Is.EqualTo(EmberFsmResult.EntryRejected));
            Assert.That(player.Context.Input.JumpRequested, Is.True); Assert.That(player.Context.Ledge.IsActive, Is.True);
            Assert.That(field.GetValue(player.Context.Ledge), Is.SameAs(before));
            Assert.That(Fsm(player).LastOperation.Attempt.CommitStarted, Is.False);
        }
        [Test] public void RuleRejectedAirLandingRetainsWallWindow()
        {
            RuleRejectTarget("air", "ground"); var player = Character(); player.Context.Sensors.Sample();
            player.Context.Wall.BeginJump(1); player.Context.Motor.SetVerticalVelocity(0);
            float before = player.Context.Wall.ProtectionRemaining;
            Assert.That(before, Is.GreaterThan(0));
            Assert.That(Fsm(player).GetActivation("air").Request(PlayerAirState.Land), Is.EqualTo(EmberFsmResult.EntryRejected));
            Assert.That(player.Context.Wall.ProtectionRemaining, Is.EqualTo(before));
            Assert.That(Fsm(player).LastOperation.Attempt.CommitStarted, Is.False);
        }
        [Test] public void RuleRejectedRollingWallFinishDoesNotSetExitFlag()
        {
            RollingSettings(); RuleRejectTarget("rolling", "ground"); var player = Character(new Vector2(0, 3)); ReachRolling(player);
            var wall = Keep(new GameObject("Rule wall")); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = player.Context.Motor.Position + new Vector2(.62f, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 4); Physics2D.SyncTransforms(); player.Context.Sensors.Sample();
            Assert.That(player.Context.Rolling.WallAhead, Is.True);
            var token = Fsm(player).GetActivation("rolling");
            Assert.That(token.Request(PlayerRollingState.Ground), Is.EqualTo(EmberFsmResult.RequestSiteRequired));
            Assert.That(token.Attempt(PlayerRollingState.Ground, "wall").Result, Is.EqualTo(EmberFsmResult.EntryRejected));
            Assert.That(typeof(PlayerRollingMotion).GetField("_wallStopped", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player.Context.Rolling), Is.False);
        }
    }
}
