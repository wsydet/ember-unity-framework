using System;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerAppearanceTests
    {
        [Test]
        public void FitUsesSpriteBoundsAndPivotWithoutChangingPhysicsAndSupportsUndo()
        {
            var root = new GameObject("AppearanceTest");
            var texture = new Texture2D(20, 40);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 20, 40), new Vector2(.2f, .1f), 10);
            try
            {
                var shape = root.AddComponent<BoxCollider2D>(); shape.size = new Vector2(2, 3); shape.offset = new Vector2(.1f, .2f);
                var sensors = root.AddComponent<EntitySensors2D>(); sensors.Configure(shape, 1);
                var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                root.AddComponent<EntityFacing2D>().Configure(visual.transform);
                Undo.IncrementCurrentGroup();
                PlayerAppearanceWindow.FitVisual(root); Undo.FlushUndoRecordObjects();
                Assert.That(renderer.bounds.size.x, Is.EqualTo(2).Within(.001));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(3).Within(.001));
                Assert.That(renderer.bounds.center.x, Is.EqualTo(.1f).Within(.001));
                Assert.That(renderer.bounds.center.y, Is.EqualTo(.2f).Within(.001));
                Assert.That(shape.size, Is.EqualTo(new Vector2(2, 3)));
                Assert.That(sensors.Shape, Is.SameAs(shape));
                Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
                Undo.PerformUndo(); Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
                Undo.PerformRedo(); Assert.That(renderer.bounds.size.y, Is.EqualTo(3).Within(.001));
                visual.AddComponent<BoxCollider2D>();
                var old = visual.transform.localScale;
                Assert.Throws<InvalidOperationException>(() => PlayerAppearanceWindow.FitVisual(root));
                Assert.That(visual.transform.localScale, Is.EqualTo(old));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
