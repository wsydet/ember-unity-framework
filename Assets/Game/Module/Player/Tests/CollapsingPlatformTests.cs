using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private CollapsingPlatform2D CollapseFixture()
        {
            var go = Keep(new GameObject("Collapse fixture")); go.SetActive(false);
            var solid = go.AddComponent<BoxCollider2D>();
            var standing = go.AddComponent<BoxCollider2D>(); standing.isTrigger = true;
            var clearance = go.AddComponent<BoxCollider2D>(); clearance.isTrigger = true;
            var platform = go.AddComponent<CollapsingPlatform2D>();
            SetAirField(platform, "_stableId", "test-platform");
            SetAirField(platform, "_solid", solid);
            SetAirField(platform, "_standingProbe", standing);
            SetAirField(platform, "_clearanceProbe", clearance);
            SetAirField(platform, "_collapseDelay", .1f);
            SetAirField(platform, "_recoveryDelay", .2f);
            go.SetActive(true);
            return platform;
        }

        private void AdvanceCollapse(CollapsingPlatform2D platform, float seconds, bool standing, bool occupied)
            => typeof(CollapsingPlatform2D).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(platform, new object[] { seconds, standing, occupied });

        [Test]
        public void CollapsingPlatform_CountdownRecoveryAndResetRespectOccupancy()
        {
            var platform = CollapseFixture();
            AdvanceCollapse(platform, .02f, false, false);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Ready));
            AdvanceCollapse(platform, .02f, true, true);
            AdvanceCollapse(platform, .1f, true, true);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Hidden));
            AdvanceCollapse(platform, .2f, false, true);
            AdvanceCollapse(platform, 1, false, true);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.WaitingForClearance));
            AdvanceCollapse(platform, .02f, false, false);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Ready));
            platform.RequestReset(); AdvanceCollapse(platform, 1, false, true);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.WaitingForClearance));
        }

        [Test]
        public void CollapsingPlatform_DestroyedProbeCannotRestoreCollision()
        {
            var platform = CollapseFixture();
            var colliders = platform.GetComponents<BoxCollider2D>();
            UnityEngine.Object.DestroyImmediate(colliders[2]);
            Assert.DoesNotThrow(() => typeof(CollapsingPlatform2D)
                .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(platform, null));
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.WaitingForClearance));
            Assert.That(colliders[0].enabled, Is.False);
        }

        [Test]
        public void CollapsingPlatform_DestroyedSolidAllowsResetAndTickWithoutException()
        {
            var platform = CollapseFixture();
            UnityEngine.Object.DestroyImmediate(platform.GetComponents<BoxCollider2D>()[0]);
            Assert.DoesNotThrow(platform.RequestReset);
            Assert.DoesNotThrow(() => typeof(CollapsingPlatform2D)
                .GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(platform, null));
        }

        [Test]
        public void CollapsingPlatform_EarlyLeaveAndNonRecoveringConfiguration()
        {
            var platform = CollapseFixture();
            SetAirField(platform, "_canRecover", false);
            AdvanceCollapse(platform, .02f, false, false);
            AdvanceCollapse(platform, .02f, true, true);
            AdvanceCollapse(platform, .01f, false, false);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Hidden));
            AdvanceCollapse(platform, 10, false, false);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Hidden));
        }
    }
}
