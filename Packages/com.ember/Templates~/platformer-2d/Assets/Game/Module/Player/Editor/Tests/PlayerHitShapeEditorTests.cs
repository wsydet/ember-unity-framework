using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerHitShapeEditorTests
    {
        [Test]
        public void NativeStageApplySupportsUndoRedoAndRejectsChangedSource()
        {
            var selected = UnityEditor.Selection.activeObject;
            var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); bool dirty = main.isDirty;
            var feel = ScriptableObject.CreateInstance<PlayerFeelProfile>();
            feel.SetAttackConfiguration(PlayerCombatConfigurationTests.Attack());
            Assert.Throws<System.InvalidOperationException>(() => PlayerHitShapePreview.Open(feel, 0, 0, 0, 0));
            string path = "Assets/HitPreviewTest_" + System.Guid.NewGuid().ToString("N") + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(feel, path);
            var testScene = main;
            try
            {
                PlayerHitShapePreview.Open(feel, 0, 0, 0, 0);
                Assert.That(testScene.isDirty, Is.EqualTo(dirty), "Open stage dirtied main scene");
                var stage = (PlayerHitShapePreview)StageUtility.GetCurrentStage();
                var collider = UnityEditor.Selection.activeGameObject.GetComponent<PolygonCollider2D>();
                collider.SetPath(0, new[] { Vector2.zero, Vector2.right * 3, Vector2.up * 2 });
                stage.ApplyCurrentPreview();
                Assert.That(testScene.isDirty, Is.EqualTo(dirty), "Apply dirtied main scene");
                Assert.That(feel.Attack.Segments[0].Windows[0].Keyframes[0].Shapes[0].Points[1].x, Is.EqualTo(3));
                UnityEditor.Undo.FlushUndoRecordObjects(); UnityEditor.Undo.PerformUndo();
                Assert.That(testScene.isDirty, Is.EqualTo(dirty), "Undo dirtied main scene");
                Assert.That(feel.Attack.Segments[0].Windows[0].Keyframes[0].Shapes[0].Points[1].x, Is.EqualTo(1));
                UnityEditor.Undo.PerformRedo();
                Assert.That(testScene.isDirty, Is.EqualTo(dirty), "Redo dirtied main scene");
                Assert.That(feel.Attack.Segments[0].Windows[0].Keyframes[0].Shapes[0].Points[1].x, Is.EqualTo(3));
                JsonUtility.FromJsonOverwrite("{\"_offset\":{\"x\":4,\"y\":0}}", feel.Attack.Segments[0].Windows[0].Keyframes[0].Shapes[0]);
                Assert.Throws<System.InvalidOperationException>(() => stage.ApplyCurrentPreview());
                Assert.That(testScene.isDirty, Is.EqualTo(dirty), "conflict dirtied main scene");
            }
            finally
            {
                if (StageUtility.GetCurrentStage() is PlayerHitShapePreview) StageUtility.GoBackToPreviousStage();
                bool dirtyAfterClose = testScene.isDirty;
                UnityEditor.Undo.ClearUndo(feel); UnityEditor.AssetDatabase.DeleteAsset(path);
                Assert.That(dirtyAfterClose, Is.EqualTo(dirty), "Closing stage dirtied main scene");
                Assert.That(testScene.isDirty, Is.EqualTo(dirty), "Destroying unsaved fixture dirtied main scene");
            }
            Assert.That(UnityEditor.Selection.activeObject, Is.EqualTo(selected)); Assert.That(main.isDirty, Is.EqualTo(dirty));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void NativeColliderEditRoundTripsWithoutTouchingMainScene(int kind)
        {
            var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); bool dirty = main.isDirty;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var shape = JsonUtility.FromJson<PlayerHitShape>("{\"_kind\":" + kind + ",\"_offset\":{\"x\":2,\"y\":3},\"_angle\":30,\"_size\":{\"x\":2,\"y\":1},\"_radius\":0.7,\"_points\":[{\"x\":0,\"y\":0},{\"x\":1,\"y\":0},{\"x\":0,\"y\":2}]}");
                var collider = PlayerHitShapeEditor.Create(shape, scene);
                collider.offset = new Vector2(.2f, .3f);
                var expected = (Vector2)collider.transform.TransformPoint(collider.offset);
                if (collider is PolygonCollider2D polygon) polygon.SetPath(0, new[] { Vector2.zero, Vector2.right * 2, Vector2.up * 3 });
                var captured = PlayerHitShapeEditor.Capture(collider);
                Assert.That(Vector2.Distance(captured.Offset, expected), Is.LessThan(.0001f));
                Assert.That(captured.Angle, Is.EqualTo(30).Within(.0001f));
                if (kind == 2) Assert.That(captured.Points[2], Is.EqualTo(Vector2.up * 3));
                var clone = PlayerHitShapeEditor.Create(captured, scene);
                Assert.That(JsonUtility.ToJson(PlayerHitShapeEditor.Capture(clone)), Is.EqualTo(JsonUtility.ToJson(captured)));
                Assert.That(shape.Offset, Is.EqualTo(new Vector2(2, 3)), "预览不修改源对象");
                var left = PlayerHitShapeEditor.Mirror(captured);
                var leftCollider = PlayerHitShapeEditor.Create(left, scene);
                var rightAgain = PlayerHitShapeEditor.Mirror(PlayerHitShapeEditor.Capture(leftCollider));
                Assert.That(Vector2.Distance(rightAgain.Offset, captured.Offset), Is.LessThan(.0001f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(rightAgain.Angle, captured.Angle)), Is.LessThan(.0001f));
                Assert.That(rightAgain.Points, Is.EqualTo(captured.Points));
                collider.transform.localScale = Vector3.one * 2;
                Assert.Throws<System.InvalidOperationException>(() => PlayerHitShapeEditor.Capture(collider));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Assert.That(main.isDirty, Is.EqualTo(dirty));
        }
    }
}
