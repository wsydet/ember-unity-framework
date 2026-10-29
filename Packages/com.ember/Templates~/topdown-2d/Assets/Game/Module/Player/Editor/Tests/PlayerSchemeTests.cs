using System;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerSchemeTests
    {
        [Test] public void ExamplesOwnWeaponGraphsAndPlayerHasNoAttackNodeOrResource()
        {
            foreach(var path in new[]{"Config/Player/PlayerScheme","Config/Player/FocusExample/PlayerScheme"})
            {
                using var captured=Resources.Load<PlayerScheme>(path).Capture();Assert.AreEqual(2,captured.Weapons.Count);
                foreach(var node in captured.Graph.Nodes)Assert.AreNotEqual("attack",node.Id);
                using var powers=Game.Entity.EntityPowerHost.Create(captured.Powers);Assert.IsFalse(powers.Skills.TryGet("attack",out _));
                Assert.IsTrue(captured.Weapons[0].LockMovement);Assert.IsFalse(captured.Weapons[1].LockMovement);Assert.AreNotSame(captured.Weapons[0].Graph,captured.Weapons[1].Graph);
            }
        }
        [Test]
        public void CopyOwnsMutableGraphAndDefinitionsAndSurvivesReload()
        {
            Assert.IsTrue(EmberProjectSetup.IsTemplateActive("topdown-2d"));
            var folder="Assets/Game/Module/Player/Editor/Tests/CopyFixture-"+Guid.NewGuid().ToString("N");
            var source=Resources.Load<PlayerScheme>("Config/Player/PlayerScheme");
            try
            {
                var copy=PlayerSchemeWorkflow.Copy(source,folder);
                Assert.AreNotEqual(source.Id,copy.Id);Assert.AreNotSame(source.Graph,copy.Graph);
                Assert.AreEqual(source.Graph.Nodes.Count,copy.Graph.Nodes.Count);
                Assert.AreEqual(2,copy.Weapons.Count);
                for(var w=0;w<source.Weapons.Count;w++)
                {
                    Assert.AreNotEqual(source.Weapons[w].Id,copy.Weapons[w].Id);Assert.AreNotSame(source.Weapons[w].Graph,copy.Weapons[w].Graph);
                    using var a=source.Weapons[w].Capture();using var b=copy.Weapons[w].Capture();
                    Assert.AreNotSame(a.Powers,b.Powers);Assert.AreNotSame(a.Graph.Nodes[0].Definition,b.Graph.Nodes[0].Definition);
                }
                for(var i=0;i<source.Graph.Nodes.Count;i++)Assert.AreNotSame(source.Graph.Nodes[i].Definition,copy.Graph.Nodes[i].Definition);
                copy.Configure(copy.Graph,13,id:copy.Id);EditorUtility.SetDirty(copy);AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(folder+"/PlayerScheme.asset",ImportAssetOptions.ForceUpdate);
                using var original=source.Capture();using var reloaded=AssetDatabase.LoadAssetAtPath<PlayerScheme>(folder+"/PlayerScheme.asset").Capture();
                Assert.AreEqual(6,original.Speed);Assert.AreEqual(13,reloaded.Speed);
                Assert.Throws<InvalidOperationException>(()=>PlayerSchemeWorkflow.Copy(source,folder));
            }
            finally {AssetDatabase.DeleteAsset(folder);}
        }

        [Test]
        public void MissingAnimationIsRejectedUnlessFallbackExplicitlyAllowed()
        {
            var source=UnityEngine.Object.Instantiate(Resources.Load<PlayerScheme>("Config/Player/PlayerScheme"));
            try
            {
                source.ConfigureAnimation(Array.Empty<PlayerAnimationMapping>(),false);
                Assert.Throws<InvalidOperationException>(()=>source.Capture());
                source.ConfigureAnimation(Array.Empty<PlayerAnimationMapping>(),true);
                using var allowed=source.Capture();Assert.IsNotNull(allowed.Graph);
            }
            finally{UnityEngine.Object.DestroyImmediate(source);}
        }
    }
}
