// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;

using NUnit.Framework;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberUPMUpgradeTrackerEditTests
    {
        [TestCase((int)EmberUPMUpgradePhase.Preparing, 1, 0.125f)]
        [TestCase((int)EmberUPMUpgradePhase.Resolving, 2, 0.375f)]
        [TestCase((int)EmberUPMUpgradePhase.Registering, 3, 0.625f)]
        [TestCase((int)EmberUPMUpgradePhase.Verifying, 4, 0.875f)]
        [TestCase((int)EmberUPMUpgradePhase.Succeeded, 4, 1f)]
        public void StagePresentation_ShouldAdvanceMonotonically(
            int phaseValue, int expectedIndex, float expectedProgress)
        {
            var phase = (EmberUPMUpgradePhase)phaseValue;
            Assert.AreEqual(expectedIndex, EmberUPMUpgradeTracker.GetStageIndex(phase));
            Assert.AreEqual(expectedProgress, EmberUPMUpgradeTracker.GetStageProgress(phase));
            Assert.IsNotEmpty(EmberUPMUpgradeTracker.GetPhaseTitle(phase));
        }

        [TestCase("0.11.0", "0.11.0", true)]
        [TestCase(" 0.11.0 ", "0.11.0", true)]
        [TestCase("0.10.0", "0.11.0", false)]
        [TestCase("", "0.11.0", false)]
        [TestCase(null, "0.11.0", false)]
        public void VersionsMatch_ShouldRequireTheInstalledTarget(
            string currentVersion, string targetVersion, bool expected)
        {
            Assert.AreEqual(expected,
                EmberUPMUpgradeTracker.VersionsMatch(currentVersion, targetVersion));
        }

        [Test]
        public void SlowThreshold_ShouldOnlyTriggerAtNinetySeconds()
        {
            var startedUtc = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);

            Assert.IsFalse(EmberUPMUpgradeTracker.HasReachedSlowThreshold(
                startedUtc, startedUtc.AddSeconds(89.99d)));
            Assert.IsTrue(EmberUPMUpgradeTracker.HasReachedSlowThreshold(
                startedUtc, startedUtc.AddSeconds(90d)));
            Assert.IsFalse(EmberUPMUpgradeTracker.HasReachedSlowThreshold(
                DateTime.MinValue, startedUtc.AddHours(1d)));
        }
    }
}
