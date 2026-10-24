using NUnit.Framework;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerCombatRequestTests
    {
        [Test]
        public void IntentAndDeadlineDoNotRefreshAcrossNaturalContinuation()
        {
            var input = new PlayerCombatRequests(.25f, .25f, .25f);
            input.Begin(PlayerAttackIntent.Normal, 1, 1);
            input.Press(PlayerCombatButton.Attack, 1);
            input.Advance(.125f);
            input.Continue(PlayerAttackIntent.Normal, .5, 1);
            input.Press(PlayerCombatButton.Attack, 1);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out var request), Is.True);
            Assert.That(request.ExpiresAt, Is.EqualTo(.25));
            input.Advance(.125f);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.False);
        }

        [Test]
        public void OrdinaryPressNeverBecomesDelayedAndOpportunityEndsFirst()
        {
            var input = new PlayerCombatRequests(1, 1, 1);
            input.Begin(PlayerAttackIntent.Normal, .125, .25);
            input.Press(PlayerCombatButton.Attack, 1);
            input.Advance(.125f);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.False);
            input.Press(PlayerCombatButton.Attack, 2);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out var special), Is.True);
            Assert.That(special.Intent, Is.EqualTo(PlayerAttackIntent.Delayed));
            input.Advance(.125f);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.False);
            input.Press(PlayerCombatButton.Attack, 3);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.False);
        }

        [Test]
        public void RejectionKeepsRequestsButCommitClearsAllOldActionRequestsOnce()
        {
            var input = new PlayerCombatRequests(1, 1, 1);
            input.Begin(PlayerAttackIntent.Normal, 1, 1);
            input.Press(PlayerCombatButton.Dash, 1); input.Press(PlayerCombatButton.Attack, 1);
            Assert.That(input.Commit(PlayerCombatButton.Attack, 2), Is.False);
            Assert.That(input.TryGet(PlayerCombatButton.Dash, out _), Is.True);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.True);
            Assert.That(input.Commit(PlayerCombatButton.Attack, 1), Is.True);
            Assert.That(input.Commit(PlayerCombatButton.Attack, 1), Is.False);
            Assert.That(input.TryGet(PlayerCombatButton.Dash, out _), Is.False);
            input.Begin(PlayerAttackIntent.DashAttack, .5, .5);
            input.Press(PlayerCombatButton.Attack, 1);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.False);
            input.Press(PlayerCombatButton.Attack, 2);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out var next), Is.True);
            Assert.That(next.Intent, Is.EqualTo(PlayerAttackIntent.DashAttack));
        }

        [Test]
        public void ZeroBufferHasOneDecisionAndClearDoesNotResetGameClock()
        {
            var input = new PlayerCombatRequests(0, 0, 0);
            input.Press(PlayerCombatButton.Attack, 1);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.True);
            input.Advance(.125f);
            Assert.That(input.TryGet(PlayerCombatButton.Attack, out _), Is.False);
            input.Clear(); Assert.That(input.Time, Is.EqualTo(.125));
        }

        [Test]
        public void AttackSpeedScalesOnlyActionClockAndCrossesShortWindows()
        {
            var clock = new PlayerActionClock(); clock.Reset(4);
            var input = new PlayerCombatRequests(1, 1, 1);
            clock.Advance(.125f); input.Advance(.125f);
            Assert.That(clock.Current, Is.EqualTo(.5));
            Assert.That(input.Time, Is.EqualTo(.125));
            Assert.That(clock.Crosses(.1, .15), Is.True);
            Assert.That(clock.Crosses(.5, .6), Is.True);
            clock.Advance(.125f);
            Assert.That(clock.Crosses(.1, .5), Is.False);
        }
    }
}
