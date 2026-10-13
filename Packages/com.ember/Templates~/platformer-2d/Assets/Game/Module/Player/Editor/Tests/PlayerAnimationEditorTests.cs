using System.Linq;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerAnimationEditorTests
    {
        [Test]
        public void AnimationAssetsReloadWithStableBindingsAndGraphIdentity()
        {
            var config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            string graph = EditorJsonUtility.ToJson(config.Graph);
            var profile = config.Animation;
            string data = EditorJsonUtility.ToJson(profile);
            var paths = profile.Slots.Select(s => AssetDatabase.GetAssetPath(s.Clip))
                .Concat(new[] { AssetDatabase.GetAssetPath(profile), AssetDatabase.GetAssetPath(profile.Controller),
                    "Assets/GameResource/Player/PlayerBlock.prefab" }).ToArray();
            var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            foreach (var path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            CollectionAssert.AreEqual(guids, paths.Select(AssetDatabase.AssetPathToGUID));
            Assert.That(EditorJsonUtility.ToJson(config.Graph), Is.EqualTo(graph));
            Assert.That(EditorJsonUtility.ToJson(config.Animation), Is.EqualTo(data));
            typeof(PlayerAnimationProfile).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(config.Animation, new object[] { config.Graph });
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Player/PlayerBlock.prefab");
            var view = prefab.GetComponentInChildren<PlayerAnimationView>(true);
            Assert.That(view.transform.parent, Is.EqualTo(view.VisualRoot));
            Assert.That(view.Sprite.transform.parent, Is.EqualTo(view.transform));
            Assert.That(view.GetComponent<Animator>().runtimeAnimatorController, Is.Null, "Prefab 不拥有第二份方案");
            Assert.That(view.GetComponent<Animator>().applyRootMotion, Is.False);
            Assert.That(prefab.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(1, 1.75f)));
        }

        [Test]
        public void AnimatedVisualFitChangesOnlyFacingOuterTransformAndSupportsUndo()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Player/PlayerBlock.prefab");
            var root = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var view = root.GetComponentInChildren<PlayerAnimationView>(true);
                var animator = view.GetComponent<Animator>();
                string animationBefore = EditorJsonUtility.ToJson(animator);
                var shape = root.GetComponent<BoxCollider2D>(); var size = shape.size;
                view.VisualRoot.localScale = new Vector3(2, 3, 1);
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                PlayerAppearanceWindow.FitVisual(root); Undo.FlushUndoRecordObjects();
                Assert.That(view.Sprite.bounds.size.x, Is.EqualTo(size.x).Within(.001));
                Assert.That(view.Sprite.bounds.size.y, Is.EqualTo(size.y).Within(.001));
                Assert.That(shape.size, Is.EqualTo(size));
                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(view.Sprite.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(EditorJsonUtility.ToJson(animator), Is.EqualTo(animationBefore));
                Undo.PerformUndo(); Assert.That(view.VisualRoot.localScale, Is.EqualTo(new Vector3(2, 3, 1)));
                Undo.RevertAllDownToGroup(group);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
