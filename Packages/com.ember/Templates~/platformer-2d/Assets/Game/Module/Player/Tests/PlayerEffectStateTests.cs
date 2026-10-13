using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [Test] public void EffectsSurviveRealGraphStateChangesAndRemainHostLocal()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerConfig).GetField("_enableEffects", flags).SetValue(_config, true);
            var definition = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            typeof(EntityEffectDefinition).GetField("_id", flags).SetValue(definition, "test.state-effect");
            typeof(EntityEffectDefinition).GetField("_duration", flags).SetValue(definition, 10f);
            var player = Character(); Land(player); var other = Character(new Vector2(10, 4));
            var effects = player.Context.Effects; effects.ApplyIndependent(definition, "test");
            other.Context.Effects.ApplyIndependent(definition, "test");
            Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("move"));
            Keys(Key.Space); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Effects, Is.SameAs(effects)); Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(other.Context.Effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.EqualTo(10));
            player.Stop(); Assert.That(effects.IsDisposed, Is.True); Assert.That(other.Context.Effects.Count, Is.EqualTo(1));
        }
    }
}
