using System;
using System.Reflection;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player.Tests
{
    internal static class PlayerDashEffectTestAssets
    {
        internal static void Set(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value); return;
            }
            throw new MissingFieldException(name);
        }
        internal static EntityEffectDefinition Install(PlayerConfig config, Action<UnityEngine.Object> keep)
        {
            Set(config, "_enableEffects", true);
            var action = ScriptableObject.CreateInstance<EntityEffectDefinition>(); keep(action);
            Set(action, "_id", "test.dash.protection"); Set(action, "_lifetime", EntityEffectLifetime.Owned);
            Set(action, "_damageCategories", new[] { "test.contact" }); Set(action, "_excludedLayers", (LayerMask)(1 << 8));
            Set(action, "_hasAppearance", true); Set(action, "_alpha", .35f);
            var tail = UnityEngine.Object.Instantiate(action); keep(tail);
            Set(tail, "_lifetime", EntityEffectLifetime.Timed); Set(tail, "_duration", 1f);
            var graph = UnityEngine.Object.Instantiate(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph")); keep(graph);
            foreach (var node in graph.Nodes)
            {
                if (!(node.Definition is PlayerGroundDashStateDefinition) && !(node.Definition is PlayerAirDashStateDefinition)) continue;
                var definition = UnityEngine.Object.Instantiate(node.Definition); keep(definition);
                if (definition is PlayerGroundDashStateDefinition ground) ground.SetCapturedEffects(action, tail);
                else ((PlayerAirDashStateDefinition)definition).SetCapturedEffects(action, tail);
                Set(node, "_definition", definition);
            }
            config.Configure(graph, config.Spawn);
            return tail;
        }
    }
}
