using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerFeelParameterTests
    {
        private PlayerFeelProfile _feel;
        private string _folder;
        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/FeelParameterTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
            _feel = ScriptableObject.CreateInstance<PlayerFeelProfile>();
            _feel.Configure(ScriptableObject.CreateInstance<PlayerMoveFeel>(), ScriptableObject.CreateInstance<PlayerJumpFeel>(), ScriptableObject.CreateInstance<PlayerInputFeel>());
            AssetDatabase.CreateAsset(_feel, _folder + "/Profile.asset");
            AssetDatabase.CreateAsset(_feel.Movement, _folder + "/Movement.asset");
            AssetDatabase.CreateAsset(_feel.Jump, _folder + "/Jump.asset");
            AssetDatabase.CreateAsset(_feel.Input, _folder + "/Input.asset");
            PlayerFsmParameterEditor.Save(_feel);
        }
        [TearDown]
        public void TearDown() { AssetDatabase.DeleteAsset(_folder); }
        private void SetSpeed(float speed)
        {
            using var serialized = new SerializedObject(_feel.Movement);
            serialized.FindProperty("_speed").floatValue = speed;
            serialized.ApplyModifiedProperties(); Undo.FlushUndoRecordObjects();
        }
        [Test]
        public void PresetCopyCreatesFourIndependentAssetsAndRefusesOverwrite()
        {
            var copy = PlayerFeelPresetWindow.SaveCopy(_feel, _folder + "/Copy.asset");
            Assert.That(copy.Movement, Is.Not.SameAs(_feel.Movement));
            Assert.That(copy.Jump, Is.Not.SameAs(_feel.Jump));
            Assert.That(copy.Input, Is.Not.SameAs(_feel.Input));
            Assert.That(PlayerFeelPresetWindow.Compare(copy, _feel), Is.Empty);
            SetSpeed(9); Assert.That(copy.Movement.Speed, Is.EqualTo(5));
            Assert.Throws<InvalidOperationException>(() => PlayerFeelPresetWindow.SaveCopy(_feel, _folder + "/Copy.asset"));
            AssetDatabase.ImportAsset(_folder + "/Copy.asset", ImportAssetOptions.ForceUpdate);
            Assert.That(AssetDatabase.LoadAssetAtPath<PlayerFeelProfile>(_folder + "/Copy.asset").Capture(Physics2D.gravity).MoveSpeed, Is.EqualTo(5));
        }
        [Test]
        public void PresetComparisonIncludesNestedParametersAndRestorePreservesIdentityAndUndo()
        {
            var reference = PlayerFeelPresetWindow.SaveCopy(_feel, _folder + "/Reference.asset");
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_feel.Movement));
            var movement = _feel.Movement; var snapshot = _feel.Capture(Physics2D.gravity);
            using (var s = new SerializedObject(_feel.Movement))
            { s.FindProperty("_airDash._speed").floatValue = 23; s.ApplyModifiedPropertiesWithoutUndo(); }
            var differences = PlayerFeelPresetWindow.Compare(_feel, reference);
            Assert.That(differences.Count, Is.EqualTo(1)); Assert.That(differences[0], Does.Contain("_airDash._speed"));
            string disk = File.ReadAllText(AssetDatabase.GetAssetPath(movement));
            PlayerFeelPresetWindow.Restore(_feel, reference);
            Assert.That(_feel.Movement, Is.SameAs(movement));
            Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(movement)), Is.EqualTo(guid));
            Assert.That(PlayerFeelPresetWindow.Compare(_feel, reference), Is.Empty);
            Undo.PerformUndo(); Assert.That(_feel.Capture(Physics2D.gravity).AirDash.Speed, Is.EqualTo(23));
            Undo.PerformRedo(); Assert.That(_feel.Capture(Physics2D.gravity).AirDash.Speed, Is.EqualTo(16));
            Assert.That(snapshot.AirDash.Speed, Is.EqualTo(16));
            Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(movement)), Is.EqualTo(disk), "恢复不自动保存");
        }
        [Test]
        public void DashGroup_TrialFreezesNestedValuesAndUndoUsesActualMovementAsset()
        {
            using var serialized = new SerializedObject(_feel.Movement);
            Undo.IncrementCurrentGroup();
            serialized.FindProperty("_airDash").FindPropertyRelative("_speed").floatValue = 21;
            serialized.ApplyModifiedProperties(); Undo.FlushUndoRecordObjects();
            string request = PlayerFeelTrialSnapshot.Capture(_feel);
            Undo.PerformUndo();
            Assert.That(_feel.Capture(Physics2D.gravity).AirDash.Speed, Is.EqualTo(16));
            var trial = PlayerFeelTrialSnapshot.Restore(request, "PlayerFeelLab", Vector2.zero, Vector2.zero);
            Assert.That(trial.Feel.AirDash.Speed, Is.EqualTo(21));
            Assert.That(trial.Feel.GroundDash.Speed, Is.EqualTo(16));
            Undo.PerformRedo();
            PlayerFsmParameterEditor.Save(_feel);
            Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(_feel.Movement)), Does.Contain("_speed: 21"));
        }
        [Test]
        public void UndoRedoEditsActualAssetButKeepsCapturedRuntimeImmutable()
        {
            var runtime = _feel.Capture(Physics2D.gravity);
            Undo.IncrementCurrentGroup(); SetSpeed(9);
            Assert.That(_feel.Movement.Speed, Is.EqualTo(9));
            Assert.That(runtime.MoveSpeed, Is.EqualTo(5));
            Undo.PerformUndo(); Assert.That(_feel.Movement.Speed, Is.EqualTo(5));
            Undo.PerformRedo(); Assert.That(_feel.Movement.Speed, Is.EqualTo(9));
        }
        [Test]
        public void TrialSnapshotFreezesUnsavedValuesWithoutWritingOrMutatingAssets()
        {
            string path = AssetDatabase.GetAssetPath(_feel.Movement);
            string disk = File.ReadAllText(path);
            SetSpeed(9); string request = PlayerFeelTrialSnapshot.Capture(_feel);
            SetSpeed(12);
            var trial = PlayerFeelTrialSnapshot.Restore(request, "PlayerFeelLab", Vector2.zero, Vector2.zero);
            Assert.That(trial.Feel.MoveSpeed, Is.EqualTo(9));
            Assert.That(_feel.Movement.Speed, Is.EqualTo(12));
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            Assert.That(EditorUtility.IsDirty(_feel.Movement), Is.True);
        }
        [Test]
        public void ExplicitSaveWritesSelectedAssetsAndRejectsInvalidConfiguration()
        {
            SetSpeed(9); PlayerFsmParameterEditor.Save(_feel);
            string path = AssetDatabase.GetAssetPath(_feel.Movement);
            string disk = File.ReadAllText(path);
            Assert.That(disk, Does.Contain("_speed: 9"));
            Assert.That(EditorUtility.IsDirty(_feel.Movement), Is.False);
            using (var serialized = new SerializedObject(_feel.Input))
            { serialized.FindProperty("_jumpBufferDuration").floatValue = -1; serialized.ApplyModifiedProperties(); }
            Assert.Throws<InvalidOperationException>(() => PlayerFsmParameterEditor.Save(_feel));
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
        }
        [Test]
        public void IncompleteTrialRequestIsRejectedWithoutFallbackToDefaults()
        {
            Assert.Throws<InvalidOperationException>(() => PlayerFeelTrialSnapshot.Restore("{}", "PlayerFeelLab", Vector2.zero, Vector2.zero));
        }
    }
}
