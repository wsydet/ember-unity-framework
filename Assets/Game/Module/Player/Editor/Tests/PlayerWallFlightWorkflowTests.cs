using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerWallFlightWorkflowTests
    {
        [Test]
        public void WallFlight_CopyValidateCompareSaveFreezeAndDefaults()
        {
            string folder = "Assets/WallFlightWorkflow_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            try
            {
                var source = PlayerSchemeOwnership.Builtins[0];
                var candidate = PlayerSchemeWorkflow.Copy(source, folder + "/A.asset");
                string movement = EditorJsonUtility.ToJson(candidate.Feel.Movement);
                candidate.Feel.Jump.ConfigureWallJump(new PlayerWallJumpSpec(
                    new PlayerOrdinaryJumpSpec(14, .02f, .2f, 120, 120, 20), 15, 10, 30, true, .05f, true));
                EditorUtility.SetDirty(candidate.Feel.Jump);
                PlayerSchemeWorkflow.Validate(candidate); PlayerSchemeWorkflow.Save(candidate);
                Assert.That(PlayerSchemeWorkflow.Compare(candidate, source).Count, Is.GreaterThan(0));
                Assert.That(EditorJsonUtility.ToJson(candidate.Feel.Movement), Is.EqualTo(movement));
                var copy = PlayerSchemeWorkflow.Copy(candidate, folder + "/B.asset");
                Assert.That(PlayerSchemeWorkflow.Compare(candidate, copy), Is.Empty);
                Assert.That(copy.Feel.Jump, Is.Not.SameAs(candidate.Feel.Jump));
                var frozen = candidate.Feel.Capture(Physics2D.gravity);
                using (var invalid = new SerializedObject(copy.Feel.Jump))
                {
                    invalid.FindProperty("_wallJump._vertical._fallGravity").floatValue = float.NaN;
                    invalid.ApplyModifiedPropertiesWithoutUndo();
                }
                Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Validate(copy));
                Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Save(copy));
                copy.Feel.Jump.ConfigureWallJump(default); EditorUtility.SetDirty(copy.Feel.Jump);
                PlayerSchemeWorkflow.Save(copy);
                Assert.That(frozen.WallJump.Vertical.LaunchSpeed, Is.EqualTo(14));
                Assert.That(candidate.Feel.Jump.WallJump.Enabled, Is.True);
                Assert.Throws<InvalidOperationException>(() => candidate.Feel.Capture(Vector2.zero));
                foreach (var builtin in PlayerSchemeOwnership.Builtins)
                { Assert.That(builtin.Feel.Jump.WallJump.Enabled, Is.False); PlayerSchemeWorkflow.Validate(builtin); }
                Assert.Throws<InvalidOperationException>(() => new PlayerFlightSpec(float.NaN, 20));
                Assert.Throws<InvalidOperationException>(() => new PlayerFlightSpec(120, 20, true, 5, 130));
                var flight = new PlayerFlightSpec(120, 20, true, 5, 60);
                var roundTrip = JsonUtility.FromJson<PlayerFlightSpec>(JsonUtility.ToJson(flight));
                Assert.That(roundTrip.FallAcceleration, Is.EqualTo(120));
                Assert.That(roundTrip.HeldAcceleration, Is.EqualTo(60));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}
