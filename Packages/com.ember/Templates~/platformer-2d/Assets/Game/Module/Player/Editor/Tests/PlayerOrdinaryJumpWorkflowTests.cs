using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerOrdinaryJumpWorkflowTests
    {
        [Test]
        public void OrdinaryJump_CopyValidateCompareSaveAndFreezeUseFormalSchemeWorkflow()
        {
            string folder = "Assets/OrdinaryJumpWorkflow_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            try
            {
                var source = PlayerSchemeOwnership.Builtins[0];
                var candidate = PlayerSchemeWorkflow.Copy(source, folder + "/A.asset");
                string movement = EditorJsonUtility.ToJson(candidate.Feel.Movement);
                var spec = new PlayerOrdinaryJumpSpec(14, .02f, .2f, 120, 120, 20,
                    apexEnabled: true, apexRequiresHeld: true);
                candidate.Feel.Jump.ConfigureOrdinaryJump(spec); EditorUtility.SetDirty(candidate.Feel.Jump);
                PlayerSchemeWorkflow.Validate(candidate); PlayerSchemeWorkflow.Save(candidate);
                Assert.That(PlayerSchemeWorkflow.Compare(candidate, source).Count, Is.GreaterThan(0));
                Assert.That(EditorJsonUtility.ToJson(candidate.Feel.Movement), Is.EqualTo(movement));
                var copy = PlayerSchemeWorkflow.Copy(candidate, folder + "/B.asset");
                Assert.That(PlayerSchemeWorkflow.Compare(candidate, copy), Is.Empty);
                var frozen = candidate.Feel.Capture(Physics2D.gravity);
                using (var invalid = new SerializedObject(copy.Feel.Jump))
                {
                    invalid.FindProperty("_ordinaryJump._riseGravity").floatValue = float.NaN;
                    invalid.ApplyModifiedPropertiesWithoutUndo();
                }
                Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Validate(copy));
                copy.Feel.Jump.ConfigureOrdinaryJump(default); EditorUtility.SetDirty(copy.Feel.Jump);
                PlayerSchemeWorkflow.Save(copy);
                Assert.That(frozen.OrdinaryJump.LaunchSpeed, Is.EqualTo(14));
                Assert.That(candidate.Feel.Jump.OrdinaryJump.Enabled, Is.True);
                var blank = PlayerSchemeWorkflow.Copy(candidate, folder + "/Blank.asset", true);
                Assert.That(blank.Feel.Jump.OrdinaryJump.Enabled, Is.False);
                foreach (var builtin in PlayerSchemeOwnership.Builtins)
                { Assert.That(builtin.Feel.Jump.OrdinaryJump.Enabled, Is.False); PlayerSchemeWorkflow.Validate(builtin); }
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}
