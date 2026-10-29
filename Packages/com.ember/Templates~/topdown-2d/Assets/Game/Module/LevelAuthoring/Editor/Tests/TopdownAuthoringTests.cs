using System;
using System.Linq;
using System.Collections.Generic;
using Ember.Core.Editor;
using Ember.LevelAuthoring.Editor;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.LevelAuthoring.Editor.Tests
{
    public sealed class TopdownAuthoringTests
    {
        private IEmberLevelAdapter Adapter=>EmberLevelAdapters.Get("topdown.level");
        [Test] public void BothLevelsHaveIndependentIdentityAndValidScopesAndSchemes()
        {
            var levels=Adapter.Discover().ToArray();Assert.AreEqual(2,levels.Length);Assert.AreNotEqual(levels[0].id,levels[1].id);Assert.AreNotEqual(levels[0].sceneGuid,levels[1].sceneGuid);
            foreach(var level in levels)Assert.IsEmpty(Adapter.Validate(level,null).Where(d=>d.Severity==EmberLevelSeverity.Error),level.displayName);
            Assert.AreNotEqual(((TopdownLevelDefinition)levels[0].configuration).Scheme.Id,((TopdownLevelDefinition)levels[1].configuration).Scheme.Id);
        }
        [Test] public void CreateLevelKeepsSourceAndGivesCopyFreshIdentityAndReloadableConfiguration()
        {
            var initial=EditorSceneManager.GetSceneManagerSetup();EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity");
            var source=Adapter.Discover().First(l=>l.id=="starter");var original=System.IO.File.ReadAllBytes(source.ScenePath);
            var name="TestCopy"+Guid.NewGuid().ToString("N");var folder="Assets/Game/Scenes/"+name;AssetDatabase.CreateFolder("Assets/Game/Scenes",name);
            var config="Assets/GameResource/Resources/Config/Level/"+name+".asset";var build=EditorBuildSettings.scenes;
            try
            {
                var copy=Adapter.Create(folder,name,source);AssetDatabase.SaveAssets();
                Assert.AreNotEqual(source.id,copy.id);Assert.AreNotEqual(source.sceneGuid,copy.sceneGuid);
                Assert.IsEmpty(Adapter.Validate(copy,null).Where(d=>d.Severity==EmberLevelSeverity.Error).Select(d=>d.Message));
                CollectionAssert.AreEqual(original,System.IO.File.ReadAllBytes(source.ScenePath));
                AssetDatabase.ImportAsset(config,ImportAssetOptions.ForceUpdate);Assert.AreEqual(copy.id,AssetDatabase.LoadAssetAtPath<TopdownLevelDefinition>(config).Id);
                Assert.Throws<InvalidOperationException>(()=>Adapter.Create(folder,name,source));
            }
            finally
            {
                EditorBuildSettings.scenes=build;AssetDatabase.DeleteAsset(folder);AssetDatabase.DeleteAsset(config);
                if(initial.Any(s=>s.isLoaded&&s.isActive&&!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(initial);
                else EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity");
            }
        }
        [Test] public void PlacementDuplicationUndoRedoRetainsUniquePersistentIdentity()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var selection=Selection.activeObject;
            try
            {
                var material=AssetDatabase.LoadAssetAtPath<EmberLevelMaterial>("Assets/Game/Module/LevelAuthoring/Editor/Materials/chaser.asset");
                var one=EmberLevelObjectEditing.Place(material,scene,Vector3.zero,Quaternion.identity,Vector3.one);
                var two=EmberLevelObjectEditing.Duplicate(one);var id=two.GetComponent<TopdownCombatActor>().StableId;
                Assert.AreNotEqual(one.GetComponent<TopdownCombatActor>().StableId,id);
                EmberLevelObjectEditing.Delete(two);Undo.PerformUndo();
                Assert.IsTrue(scene.GetRootGameObjects().Any(g=>g.GetComponent<TopdownCombatActor>()?.StableId==id));
                Undo.PerformRedo();Assert.IsFalse(scene.GetRootGameObjects().Any(g=>g.GetComponent<TopdownCombatActor>()?.StableId==id));
            }
            finally{EditorSceneManager.CloseScene(scene,true);Selection.activeObject=selection;}
        }
        [Test] public void BlueprintRegistrationRejectsUnrelatedOwnerAndDisabledModule()
        {
            Assert.IsTrue(EmberFsmBlueprintAccess.Allows("topdown-2d",()=>true));
            Assert.IsFalse(EmberFsmBlueprintAccess.Allows("platformer-2d",()=>true));
            Assert.IsFalse(EmberFsmBlueprintAccess.Allows("topdown-2d",()=>false));
            Assert.IsFalse(EmberFsmBlueprintAccess.Allows("unknown-template",()=>true));
        }
        [Test] public void FormalTopdownIdentityMatrixIncludesChildrenConsumerAndInvalidChains()
        {
            var evaluate=typeof(EmberProjectSetup).GetMethod("IsTemplateActiveForIdentity",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(evaluate);
            var templates=new List<TemplateInfo>{new(){id="base"},new(){id="topdown-2d",parentId="base"},new(){id="topdown-child",parentId="topdown-2d"},
                new(){id="platformer-2d",parentId="base"},new(){id="visual-novel",parentId="base"},new(){id="broken",parentId="missing"},new(){id="cycle",parentId="cycle"}};
            foreach(var embedded in new[]{true,false})
            foreach(var active in new[]{"topdown-2d","topdown-child","base","platformer-2d","visual-novel","missing","broken","cycle",null})
                Assert.AreEqual(active=="topdown-2d"||active=="topdown-child",evaluate.Invoke(null,new object[]{"topdown-2d",embedded,embedded?active:null,embedded?null:active,templates}),$"embedded={embedded}, active={active}");
        }
    }
}
