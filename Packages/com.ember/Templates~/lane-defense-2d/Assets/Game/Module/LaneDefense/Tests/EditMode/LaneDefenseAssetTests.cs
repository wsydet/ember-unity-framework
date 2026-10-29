using System.Collections.Generic;
using System.Linq;
using Ember.UIExtension;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.LaneDefense.Tests
{
    public sealed class LaneDefenseAssetTests
    {
        [TestCase("EUILaneBattleHUD")]
        [TestCase("EUILaneCardItem")]
        [TestCase("EUILanePausePopup")]
        [TestCase("EUILaneResultPopup")]
        public void AuthoredBindingsPopulateAllRuntimeControls(string name)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Module/LaneDefense/Prefabs/"+name+".prefab");Assert.That(asset,Is.Not.Null);
            var instance=Object.Instantiate(asset);
            try
            {
                var binding=instance.GetComponent<EUIBinding>();var map=new Dictionary<string,Component>();EUIBindingBridge.PopulateControlMap(binding,map);
                Assert.That(binding.Bindings.Count,Is.GreaterThan(0));
                foreach(var entry in binding.Bindings){Assert.That(map.ContainsKey(entry.Name),Is.True,entry.Name);Assert.That(map[entry.Name],Is.Not.Null,entry.Name);}
            }
            finally{Object.DestroyImmediate(instance);}
        }
        [TestCase("TrainingYard",5,9,2)]
        [TestCase("CompactYard",3,6,3)]
        public void ExampleConfigurationsAreValidAndDifferent(string id,int rows,int columns,int waves)
        {
            var level=AssetDatabase.LoadAssetAtPath<LaneDefenseLevel>("Assets/GameResource/Resources/Config/LaneDefense/"+id+".asset");Assert.That(level,Is.Not.Null);Assert.That(level.Validate(),Is.Empty);
            var snapshot=level.Freeze();Assert.That(snapshot.Rows,Is.EqualTo(rows));Assert.That(snapshot.Columns,Is.EqualTo(columns));Assert.That(snapshot.Waves.Count,Is.EqualTo(waves));
        }
        [Test] public void ExampleRenderPrefabsHaveImportedSprites()
        {
            foreach(string name in new[]{"Cell","Unit","Enemy","Projectile"})
            {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Prefabs/LaneDefense/"+name+".prefab");Assert.That(prefab.GetComponent<SpriteRenderer>().sprite,Is.Not.Null,name);}
        }
    }
}
