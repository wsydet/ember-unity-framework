using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerSchemeObservationTests
    {
        private static string Describe(PlayerSchemeObservation observation, PlayerScheme scheme) =>
            (string)typeof(PlayerSchemeWindow).Assembly.GetType("Game.Player.Editor.PlayerSchemeObservationEditor")
                .GetMethod("Describe", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { observation, scheme });
        [Test]
        public void ObservationRetainsCaptureAcrossEditsUndoAndRecovery()
        {
            string folder = "Assets/SchemeObservationTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            try
            {
                var source = PlayerSchemeWorkflow.Copy(PlayerSchemeOwnership.Builtins[0], folder + "/Custom.asset");
                PlayerSchemeConversion.SelectScheme(config, source, false);
                using var capture = new PlayerSchemeCapture(config);
                var observation = capture.Config.SchemeObservation;
                Assert.That(capture.Config.Scheme, Is.Null);
                Assert.That(observation.Source, Is.SameAs(source));
                Assert.That(Describe(observation, source), Does.Contain("与捕获时一致"));
                float frozen = capture.Config.CapturedFeel.MoveSpeed;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                using (var data = new SerializedObject(source.Feel.Movement))
                {
                    data.FindProperty("_speed").floatValue = frozen + 2;
                    data.ApplyModifiedProperties();
                }
                Undo.FlushUndoRecordObjects();
                Assert.That(Describe(observation, source), Does.Contain("编辑配置已变化"));
                Assert.That(capture.Config.CapturedFeel.MoveSpeed, Is.EqualTo(frozen));
                using (var retained = capture.Retain())
                    Assert.That(retained.Config.SchemeObservation, Is.SameAs(observation));
                Undo.RevertAllDownToGroup(group);
                Assert.That(Describe(observation, source), Does.Contain("与捕获时一致"));
                Assert.That(Describe(observation, PlayerSchemeOwnership.Builtins[1]), Does.Contain("与编辑方案不同"));
                using var next = new PlayerSchemeCapture(config);
                Assert.That(next.Config.SchemeObservation.CaptureId, Is.Not.EqualTo(observation.CaptureId));
            }
            finally { UnityEngine.Object.DestroyImmediate(config); AssetDatabase.DeleteAsset(folder); }
        }

        [Test]
        public void ObservationFailureDoesNotBreakRuntimeCapture()
        {
            var provider = PlayerSchemeObservation.FingerprintProvider;
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            try
            {
                PlayerSchemeConversion.SelectScheme(config, PlayerSchemeOwnership.Builtins[0], false);
                PlayerSchemeObservation.FingerprintProvider = _ => throw new InvalidOperationException("observer failure");
                using var capture = new PlayerSchemeCapture(config);
                Assert.That(capture.Config.CapturedFeel.MoveSpeed, Is.GreaterThan(0));
                Assert.That(capture.Config.SchemeObservation.Fingerprint, Is.Null);
                Assert.That(Describe(capture.Config.SchemeObservation, config.Scheme), Does.Contain("无法核对"));
            }
            finally { PlayerSchemeObservation.FingerprintProvider = provider; UnityEngine.Object.DestroyImmediate(config); }
        }
    }
}
