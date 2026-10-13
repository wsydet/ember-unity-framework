using System;
using Ember.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Game.Input.Tests
{
    public sealed class GameInputTests : InputTestFixture
    {
        private InputActionAsset _actions;
        private GameInputConfig _config;
        private GameInputService _service;
        private GameInputChannel _play;
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            _actions.devices = new InputDevice[] { _keyboard };
            var play = _actions.AddActionMap("Play");
            play.AddAction("Trigger", InputActionType.Button, "<Keyboard>/space");
            play.AddAction("Axis", InputActionType.Value, "<Keyboard>/d", expectedControlLayout: "Axis");
            _actions.AddActionMap("Menus").AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
            _config = ScriptableObject.CreateInstance<GameInputConfig>();
            _config.Configure(_actions, new GameInputChannelConfig("play", "Play"), new GameInputChannelConfig("menu", "Menus", true));
            _service = new GameInputService(_config, _actions);
            _play = _service.GetChannel("play");
        }

        public override void TearDown()
        {
            try
            {
                _service?.Dispose();
                if (_actions != null)
                {
                    _actions.Disable();
                    Object.DestroyImmediate(_actions);
                }
                if (_config != null) Object.DestroyImmediate(_config);
            }
            finally { base.TearDown(); }
        }

        private GameInputRequest Request(int priority, GameInputPermission permission, object owner = null)
            => _service.Request(owner ?? this, priority, new GameInputRule("play", permission));

        private void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        [Test]
        public void DefaultsApplyIndependentlyAndGameplayStartsClosed()
        {
            Assert.That(_play.IsAvailable, Is.False);
            Assert.That(_actions.FindActionMap("Play").enabled, Is.False);
            Assert.That(_service.GetChannel("menu").IsAvailable, Is.True);
            Assert.That(_actions.FindActionMap("Menus").enabled, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EqualPriorityDenyWinsRegardlessOfArrival(bool denyFirst)
        {
            using var first = Request(4, denyFirst ? GameInputPermission.Deny : GameInputPermission.Allow);
            using var second = Request(4, denyFirst ? GameInputPermission.Allow : GameInputPermission.Deny);
            Assert.That(_play.IsAvailable, Is.False);
        }

        [Test]
        public void HighestExplicitRequestWinsAndUnchangedDoesNotMaskIt()
        {
            using var low = Request(int.MinValue, GameInputPermission.Allow);
            Assert.That(_play.IsAvailable, Is.True);
            using var deny = Request(3, GameInputPermission.Deny);
            using var ignore = Request(int.MaxValue, GameInputPermission.Unchanged);
            Assert.That(_play.IsAvailable, Is.False);
            using var high = Request(4, GameInputPermission.Allow);
            Assert.That(_play.IsAvailable, Is.True);
        }

        [Test]
        public void ClosingMenuDoesNotReleaseCutsceneOrRestoreHistoricalMode()
        {
            using var session = Request(0, GameInputPermission.Allow);
            using var cutscene = Request(10, GameInputPermission.Deny, new object());
            var menu = Request(20, GameInputPermission.Deny, new object());
            menu.Dispose();
            menu.Dispose();
            Assert.That(_play.IsAvailable, Is.False);
            cutscene.Dispose();
            Assert.That(_play.IsAvailable, Is.True);
            session.Dispose();
            Assert.That(_play.IsAvailable, Is.False);
        }

        [Test]
        public void ReleaseOwnerOnlyReleasesThatReferenceAndHandlesBecomeInvalid()
        {
            var owner = new object();
            var request = Request(20, GameInputPermission.Deny, owner);
            using var session = Request(0, GameInputPermission.Allow, new object());
            _service.ReleaseOwner(owner);
            Assert.That(request.IsReleased, Is.True);
            Assert.That(_play.IsAvailable, Is.True);
            Assert.Throws<ObjectDisposedException>(() => request.Update(0));
        }

        [Test]
        public void RequestSnapshotsRulesAndValidatesUpdateBeforeChangingExistingRequest()
        {
            var rules = new[] { new GameInputRule("play", GameInputPermission.Allow) };
            using var request = _service.Request(this, 0, rules);
            rules[0] = new GameInputRule("play", GameInputPermission.Deny);
            Assert.That(_play.IsAvailable, Is.True);
            Assert.Throws<ArgumentException>(() => request.Update(100, new GameInputRule("missing", GameInputPermission.Deny)));
            Assert.That(request.Priority, Is.EqualTo(0));
            Assert.That(_play.IsAvailable, Is.True);
            request.Update(3, new GameInputRule("play", GameInputPermission.Deny));
            Assert.That(_play.IsAvailable, Is.False);
        }

        [Test]
        public void NotificationsOnlyFireForEffectiveChangesAndCanReleaseOtherRequests()
        {
            using var allow = Request(0, GameInputPermission.Allow);
            var deny = Request(5, GameInputPermission.Deny);
            int count = 0;
            _play.AvailabilityChanged += channel => { count++; if (channel.IsAvailable) deny.Dispose(); };
            using var stronger = Request(10, GameInputPermission.Allow);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(deny.IsReleased, Is.True);
            stronger.Dispose();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidConfigurationDoesNotDisableExistingMaps()
        {
            _config.Configure(_actions, new GameInputChannelConfig("one", "Menus"), new GameInputChannelConfig("two", "Menus"));
            Assert.Throws<ArgumentException>(() => new GameInputService(_config, _actions));
            Assert.That(_actions.FindActionMap("Menus").enabled, Is.True);
        }

        [Test]
        public void ConfigurationIsFrozenForRunningService()
        {
            _config.Configure(_actions, new GameInputChannelConfig("replacement", "Play", true));
            Assert.That(_play.IsAvailable, Is.False);
            Assert.Throws<ArgumentException>(() => _service.GetChannel("replacement"));
        }

        [Test]
        public void TriggerIsConsumedOnceAndSurvivesUntilConsumerTick()
        {
            using var button = new GameInputButton(_play, "Trigger");
            using var allow = Request(0, GameInputPermission.Allow);
            Keys(Key.Space);
            Keys();
            InputSystem.Update();
            Assert.That(button.ConsumePress(), Is.True);
            Assert.That(button.ConsumePress(), Is.False);
            Assert.That(button.IsHeld, Is.False);
        }

        [Test]
        public void DisableClearsUnconsumedPressImmediately()
        {
            using var button = new GameInputButton(_play, "Trigger");
            using var allow = Request(0, GameInputPermission.Allow);
            Keys(Key.Space);
            using var deny = Request(1, GameInputPermission.Deny);
            Assert.That(button.ConsumePress(), Is.False);
            Assert.That(button.IsHeld, Is.False);
        }

        [Test]
        public void HeldAcrossResumeCannotTriggerOrStartChargeUntilReleasedAndPressed()
        {
            using var button = new GameInputButton(_play, "Trigger");
            Keys(Key.Space);
            using var allow = Request(0, GameInputPermission.Allow);
            InputSystem.Update();
            Assert.That(button.ConsumePress(), Is.False);
            Assert.That(button.IsHeld, Is.False);
            Keys();
            Keys(Key.Space);
            Assert.That(button.ConsumePress(), Is.True);
            Assert.That(button.IsHeld, Is.True);
        }

        [Test]
        public void MenuOnlyPressAndReleaseIsNeverBuffered()
        {
            using var button = new GameInputButton(_play, "Trigger");
            Keys(Key.Space);
            Keys();
            using var allow = Request(0, GameInputPermission.Allow);
            InputSystem.Update();
            Assert.That(button.ConsumePress(), Is.False);
            Keys(Key.Space);
            Assert.That(button.ConsumePress(), Is.True);
        }

        [Test]
        public void ContinuousInputRecoversHeldValueWhileTriggerDoesNot()
        {
            using var button = new GameInputButton(_play, "Trigger");
            Keys(Key.D, Key.Space);
            Assert.That(_play.ReadContinuous<float>("Axis"), Is.Zero);
            using var allow = Request(0, GameInputPermission.Allow);
            InputSystem.Update();
            Assert.That(_play.ReadContinuous<float>("Axis"), Is.EqualTo(1f));
            Assert.That(button.ConsumePress(), Is.False);
        }

        [Test]
        public void IndependentReadersDoNotConsumeEachOthersPress()
        {
            using var first = new GameInputButton(_play, "Trigger");
            using var second = new GameInputButton(_play, "Trigger");
            using var allow = Request(0, GameInputPermission.Allow);
            Keys(Key.Space);
            Assert.That(first.ConsumePress(), Is.True);
            Assert.That(second.ConsumePress(), Is.True);
        }

        [Test]
        public void PressAndReleaseInOneInputUpdateStillProducesOnePress()
        {
            using var button = new GameInputButton(_play, "Trigger");
            using var allow = Request(0, GameInputPermission.Allow);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(button.ConsumePress(), Is.True);
            Assert.That(button.IsHeld, Is.False);
        }

        [Test]
        public void HeldReleaseAndRepressDuringFirstResumedUpdateRequiresAndAcceptsNewPress()
        {
            using var button = new GameInputButton(_play, "Trigger");
            Keys(Key.Space);
            using var allow = Request(0, GameInputPermission.Allow);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            Assert.That(button.ConsumePress(), Is.True);
            Assert.That(button.ConsumePress(), Is.False);
        }

        [Test]
        public void ReaderCreatedWhileHeldAlsoRequiresReleaseAndRepress()
        {
            using var allow = Request(0, GameInputPermission.Allow);
            Keys(Key.Space);
            using var button = new GameInputButton(_play, "Trigger");
            InputSystem.Update();
            Assert.That(button.ConsumePress(), Is.False);
            Assert.That(button.IsHeld, Is.False);
            Keys();
            Keys(Key.Space);
            Assert.That(button.ConsumePress(), Is.True);
        }

        [Test]
        public void StopInsideAvailabilityNotificationCannotReenableAnotherChannel()
        {
            _play.AvailabilityChanged += channel => _service.Dispose();
            var request = _service.Request(this, 0,
                new GameInputRule("play", GameInputPermission.Allow),
                new GameInputRule("menu", GameInputPermission.Deny));
            Assert.That(request.IsReleased, Is.True);
            Assert.That(_actions.FindActionMap("Play").enabled, Is.False);
            Assert.That(_actions.FindActionMap("Menus").enabled, Is.False);
        }

        [Test]
        public void BundledDefaultConfigurationLoadsAndKeepsGameplayClosed()
        {
            var config = Resources.Load<GameInputConfig>(GameInputModule.CONFIG_PATH);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Actions, Is.Not.Null);
            var actions = Object.Instantiate(config.Actions);
            try
            {
                using var service = new GameInputService(config, actions);
                Assert.That(service.GetChannel("gameplay").IsAvailable, Is.False);
                Assert.That(service.GetChannel("ui").IsAvailable, Is.True);
                Assert.That(service.GetChannel("gameplay").GetAction("Direction"), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(actions); }
        }

        [Test]
        public void StopClosesChannelsClearsReadersAndInvalidatesOldHandles()
        {
            using var button = new GameInputButton(_play, "Trigger");
            var allow = Request(0, GameInputPermission.Allow);
            Keys(Key.Space);
            _service.Dispose();
            _service.Dispose();
            Assert.That(allow.IsReleased, Is.True);
            Assert.That(_play.IsAvailable, Is.False);
            Assert.That(_actions.FindActionMap("Menus").enabled, Is.False);
            Assert.That(button.ConsumePress(), Is.False);
            Assert.Throws<ObjectDisposedException>(() => _service.Request(this, 0));
            allow.Dispose();
        }

        [Test]
        public void ModuleInitializationIsExplicitAndResetCannotReviveOldRequests()
        {
            _service.Dispose();
            var module = new GameInputModule();
            var lifecycle = (IEmberModule)module;
            try
            {
                Assert.That(module.WhenReady.Result.Status, Is.EqualTo(GameInputPrepareStatus.NotInitialized));
                Assert.That(module.Service, Is.Null);
                module.SetConfigurationForNextInit(_config, _actions);
                Assert.That(module.State, Is.EqualTo(GameInputServiceState.Uninitialized));
                lifecycle.OnInit();
                Assert.That(module.WhenReady.Result.Succeeded, Is.True);
                var firstEpoch = module.Epoch;
                var old = module.Service.Request(this, 0, new GameInputRule("play", GameInputPermission.Allow));
                lifecycle.OnDestroy();
                lifecycle.ResetModuleData();
                lifecycle.OnInit();
                Assert.That(module.Epoch, Is.GreaterThan(firstEpoch));
                old.Dispose();
                Assert.That(module.Service.GetChannel("play").IsAvailable, Is.False);
                Assert.That(module.WhenReady.Result.Epoch, Is.EqualTo(module.Epoch));
            }
            finally { lifecycle.OnDestroy(); }
        }

        [Test]
        public void ModuleConfigurationFailureCompletesReadinessWithExplicitFailure()
        {
            var module = new GameInputModule();
            module.SetConfigurationForNextInit(null);
            ((IEmberModule)module).OnInit();
            Assert.That(module.State, Is.EqualTo(GameInputServiceState.Faulted));
            Assert.That(module.WhenReady.IsCompleted, Is.True);
            Assert.That(module.WhenReady.Result.Status, Is.EqualTo(GameInputPrepareStatus.ConfigInvalid));
            Assert.That(module.Service, Is.Null);
            ((IEmberModule)module).OnDestroy();
        }
    }
}
