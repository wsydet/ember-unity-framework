using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;

namespace Game.Player.Tests
{
    public sealed class PlayerActionEffectCaptureTests
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private PlayerConfig _config;
        private PlayerScheme _scheme;
        private PlayerActionEffects _effects;
        private T Keep<T>(T value) where T : UnityEngine.Object { _objects.Add(value); return value; }
        private static void Set(object target, string name, object value) => PlayerDashEffectTestAssets.Set(target, name, value);
        [SetUp] public void Setup()
        {
            _config = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
            _scheme = Keep(UnityEngine.Object.Instantiate(_config.Scheme)); Set(_config, "_scheme", _scheme);
            _effects = Keep(ScriptableObject.CreateInstance<PlayerActionEffects>());
            _effects.Configure(new[] { new PlayerActionEffectSlot("ground-dash", null, null), new PlayerActionEffectSlot("air-dash", null, null) });
            Set(_scheme, "_actionEffects", _effects);
        }
        [TearDown] public void Cleanup()
        { foreach (var value in _objects) if (value != null) UnityEngine.Object.DestroyImmediate(value); _objects.Clear(); }
        private EntityEffectDefinition Effect(string id)
        {
            var effect = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            Set(effect, "_id", id); Set(effect, "_lifetime", EntityEffectLifetime.Owned); return effect;
        }
        [TestCase("missing")] [TestCase("version")] [TestCase("duplicate")] [TestCase("unknown")]
        [TestCase("unsupported")] [TestCase("incomplete")] [TestCase("lifetime")]
        public void InvalidWiringIsRejectedBeforeRuntime(string mode)
        {
            if (mode == "missing") Set(_scheme, "_actionEffects", null);
            if (mode == "version") Set(_effects, "_version", 0);
            if (mode == "duplicate") _effects.Configure(new[] { new PlayerActionEffectSlot("air-dash", null, null), new PlayerActionEffectSlot("air-dash", null, null) });
            if (mode == "unknown") _effects.Configure(new[] { new PlayerActionEffectSlot("absent", null, null) });
            if (mode == "unsupported") _effects.Configure(new[] { new PlayerActionEffectSlot("idle", null, null) });
            if (mode == "incomplete") _effects.Configure(Array.Empty<PlayerActionEffectSlot>());
            if (mode == "lifetime") _effects.Configure(new[] { new PlayerActionEffectSlot("air-dash", null, Effect("invalid.tail")) });
            Assert.That(() => { using var capture = new PlayerSchemeCapture(_config); }, Throws.Exception);
        }
        [Test] public void SharedDefinitionIsCapturedPerNodeAndSharedEffectsStaySharedOnlyWithinCapture()
        {
            var graph = Keep(UnityEngine.Object.Instantiate(_scheme.Graph));
            var common = graph.Nodes.First(n => n.Id == "ground-dash").Definition;
            graph.Configure(graph.EntryId, graph.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                n.Id == "air-dash" ? common : n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.ToArray())).ToArray());
            Set(_scheme, "_graph", graph);
            var a = Effect("effect.a"); var b = Effect("effect.b");
            _effects.Configure(new[] { new PlayerActionEffectSlot("ground-dash", a, null), new PlayerActionEffectSlot("air-dash", b, null) });
            using var first = new PlayerSchemeCapture(_config);
            var ground = (PlayerGroundDashStateDefinition)first.Config.Graph.Nodes.First(n => n.Id == "ground-dash").Definition;
            var air = (PlayerGroundDashStateDefinition)first.Config.Graph.Nodes.First(n => n.Id == "air-dash").Definition;
            Assert.That(ground, Is.Not.SameAs(air)); Assert.That(ground.ActionEffect.Id, Is.EqualTo("effect.a"));
            Assert.That(air.ActionEffect.Id, Is.EqualTo("effect.b")); Assert.That(((PlayerGroundDashStateDefinition)common).ActionEffect, Is.Null);
            _effects.Configure(new[] { new PlayerActionEffectSlot("ground-dash", a, null), new PlayerActionEffectSlot("air-dash", a, null) });
            using var second = new PlayerSchemeCapture(_config);
            var g2 = (PlayerGroundDashStateDefinition)second.Config.Graph.Nodes.First(n => n.Id == "ground-dash").Definition;
            var a2 = (PlayerGroundDashStateDefinition)second.Config.Graph.Nodes.First(n => n.Id == "air-dash").Definition;
            Assert.That(g2.ActionEffect, Is.SameAs(a2.ActionEffect)); Assert.That(g2.ActionEffect, Is.Not.SameAs(ground.ActionEffect));
            Assert.That(air.ActionEffect.Id, Is.EqualTo("effect.b"));
        }
        [Test] public void ExplicitEmptyNeverReadsLegacyAndCaptureReleaseDoesNotDestroySource()
        {
            var graph = Keep(UnityEngine.Object.Instantiate(_scheme.Graph));
            var definition = Keep(UnityEngine.Object.Instantiate(graph.Nodes.First(n => n.Id == "ground-dash").Definition));
            Set(definition, "_actionEffect", Effect("legacy.must.not.run"));
            graph.Configure(graph.EntryId, graph.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                n.Id == "ground-dash" ? definition : n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.ToArray())).ToArray());
            Set(_scheme, "_graph", graph);
            var capture = new PlayerSchemeCapture(_config);
            var result = (PlayerGroundDashStateDefinition)capture.Config.Graph.Nodes.First(n => n.Id == "ground-dash").Definition;
            Assert.That(result.ActionEffect, Is.Null); Assert.That(result.TailEffect, Is.Null);
            capture.Dispose(); Assert.That(result == null, Is.True); Assert.That(definition != null, Is.True);
        }
    }
}
