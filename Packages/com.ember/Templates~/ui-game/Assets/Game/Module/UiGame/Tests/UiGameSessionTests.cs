using NUnit.Framework;

namespace Game.UiGame.Tests
{
    public sealed class UiGameSessionTests
    {
        [Test]
        public void DuplicateStepAndCompletionAreRejected()
        {
            using var session = new UiGameSession(1);
            var changes = 0;
            session.Changed += () => changes++;
            Assert.That(session.TryAdvance(0), Is.True);
            Assert.That(session.TryAdvance(0), Is.False);
            Assert.That(session.TryAdvance(2), Is.False);
            Assert.That(session.TryAdvance(1), Is.True);
            Assert.That(session.TryAdvance(2), Is.True);
            Assert.That(session.TryAdvance(3), Is.False);
            Assert.That(changes, Is.EqualTo(3));
            Assert.That(session.IsComplete, Is.True);
        }

        [Test]
        public void PauseCannotReleaseNavigationOrCompletion()
        {
            using var session = new UiGameSession(1);
            session.SetPaused(true);
            Assert.That(session.TryAdvance(0), Is.False);
            Assert.That(session.TryBeginNavigation(), Is.False);
            session.SetPaused(false);
            Assert.That(session.TryBeginNavigation(), Is.True);
            session.SetPaused(true);
            session.SetPaused(false);
            Assert.That(session.CanAdvance, Is.False);
            session.CancelNavigation();
            for (var i = 0; i < 3; i++) Assert.That(session.TryAdvance(i), Is.True);
            session.SetPaused(true);
            session.SetPaused(false);
            Assert.That(session.CanAdvance, Is.False);
        }

        [Test]
        public void RestartRejectsOldSessionAndReleasesSubscriptions()
        {
            var module = new UiGameModule();
            module.OnInit();
            var old = module.Session;
            var notifications = 0;
            old.Changed += () => notifications++;
            Assert.That(module.TryRestart(old), Is.False);
            for (var i = 0; i < 3; i++) old.TryAdvance(i);
            Assert.That(module.TryRestart(old), Is.True);
            Assert.That(module.TryRestart(old), Is.False);
            Assert.That(old.IsDisposed, Is.True);
            old.SetPaused(true);
            old.CancelNavigation();
            Assert.That(old.TryAdvance(0), Is.False);
            Assert.That(notifications, Is.EqualTo(4));
            Assert.That(module.Session.Generation, Is.GreaterThan(old.Generation));
            Assert.That(module.Session.CompletedSteps, Is.Zero);
            ((Ember.Core.IEmberModule)module).OnDestroy();
            Assert.That(module.CreatedSessions, Is.EqualTo(module.ReleasedSessions));
        }

        [Test]
        public void RepeatedEnterExitHasOneOwnerAndNoLiveOldSession()
        {
            var module = new UiGameModule();
            for (var i = 0; i < 8; i++)
            {
                module.OnInit();
                var session = module.Session;
                Assert.That(module.CreatedSessions - module.ReleasedSessions, Is.EqualTo(1));
                ((Ember.Core.IEmberModule)module).OnDestroy();
                ((Ember.Core.IEmberModule)module).OnDestroy();
                Assert.That(module.IsCurrent(session), Is.False);
                Assert.That(session.TryAdvance(0), Is.False);
                Assert.That(module.CreatedSessions, Is.EqualTo(module.ReleasedSessions));
            }
        }
    }
}
