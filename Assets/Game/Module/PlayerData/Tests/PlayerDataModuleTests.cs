using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ember.Core;
using Game.Module;
using NUnit.Framework;
using UnityEngine;
using ProgressModel = Game.Module.PlayerData;

namespace Game.PlayerData.Tests
{
    public sealed class PlayerDataModuleTests
    {
        #region 内部参数
        private PlayerDataModule _module;
        private IEmberModule _lifecycle;
        private PlayerDataConfig _config;
        private PlayerDataCodec _codec;
        private TestFiles _files;
        private string _root;
        private PumpContext _context;
        private SynchronizationContext _previous;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void WaitUntil(Func<bool> predicate)
        {
            var watch = Stopwatch.StartNew();
            while (!predicate())
            {
                if (watch.Elapsed.TotalSeconds > 10) Assert.Fail("Controlled operation timed out.");
                Thread.Sleep(1);
            }
        }

        private void Await(Task task)
        {
            WaitUntil(() => { _context.Pump(); return task.IsCompleted; });
            task.GetAwaiter().GetResult(); _context.Pump();
        }

        private T Await<T>(Task<T> task) { Await((Task)task); return task.GetAwaiter().GetResult(); }
        private void Ready() { _lifecycle.OnInit(); Assert.That(Await(_module.WhenReady).Succeeded, Is.True); }
        private SaveOperation<SaveResult> Save(int coins, int slot = 0) => _module.SaveAsync(slot,
            new ProgressModel { level = 2, coins = coins }, new SaveSummary("test"), _codec);
        private ReadResult<ProgressModel> Read(int slot = 0) => Await(_module.ReadAsync(slot, _codec).Completion);

        private sealed class PumpContext : SynchronizationContext
        {
            private readonly ConcurrentQueue<Action> _queue = new();
            public override void Post(SendOrPostCallback callback, object state) => _queue.Enqueue(() => callback(state));
            public override SynchronizationContext CreateCopy() => this;
            internal void Pump() { while (_queue.TryDequeue(out var action)) action(); }
        }

        private sealed class TestFiles : ProgressFileIO
        {
            internal readonly ManualResetEventSlim Entered = new(false);
            internal readonly ManualResetEventSlim Release = new(true);
            internal int BlockPrepare;
            internal int BlockSave;
            internal volatile bool FailSave;
            private void Block()
            {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(8))) throw new IOException("Test barrier timeout.");
            }
            internal override byte[] ReadBounded(string path, int maximumBytes)
            {
                if (path.EndsWith("index.json", StringComparison.Ordinal) && Interlocked.Exchange(ref BlockPrepare, 0) == 1) Block();
                return base.ReadBounded(path, maximumBytes);
            }
            internal override void WriteNewAndFlush(string path, byte[] bytes)
            {
                if (path.EndsWith(".progress.tmp", StringComparison.Ordinal) && Interlocked.Exchange(ref BlockSave, 0) == 1) Block();
                if (FailSave) throw new IOException("injected disk failure");
                base.WriteNewAndFlush(path, bytes);
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _previous = SynchronizationContext.Current;
            _context = new PumpContext(); SynchronizationContext.SetSynchronizationContext(_context);
            _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberModuleSaveTests", Guid.NewGuid().ToString("N")));
            _config = ScriptableObject.CreateInstance<PlayerDataConfig>();
            _config.Configure("TestProgress", "ember.base.progress", new[] { 0, 1 });
            _codec = new PlayerDataCodec();
            _files = new TestFiles();
            _module = new PlayerDataModule { FilesForNextInit = _files };
            _module.SetConfigurationForNextInit(_config, _root);
            _lifecycle = _module;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _files.Release.Set(); Await(_module.StopAsync());
                string boundary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberModuleSaveTests")) + Path.DirectorySeparatorChar;
                Assert.That(_root.StartsWith(boundary, StringComparison.OrdinalIgnoreCase), Is.True);
                if (Directory.Exists(_root)) Directory.Delete(_root, true);
                UnityEngine.Object.DestroyImmediate(_config);
            }
            finally { SynchronizationContext.SetSynchronizationContext(_previous); _files.Entered.Dispose(); _files.Release.Dispose(); }
        }

        [Test]
        public void ConstructionAndConfigurationDoNotActivateOrTouchDisk()
        {
            Assert.That(_module.State, Is.EqualTo(SaveServiceState.Uninitialized));
            Assert.That(_module.WhenReady.Result.Status, Is.EqualTo(PrepareStatus.NotInitialized));
            Assert.That(_module.GetSlots().State, Is.EqualTo(SaveServiceState.Uninitialized));
            Assert.That(Save(1).Completion.Result.Status, Is.EqualTo(SaveStatus.NotReady));
            Assert.That(Read().Status, Is.EqualTo(ReadStatus.NotReady));
            Assert.That(Directory.Exists(_root), Is.False);
            Assert.That(typeof(PlayerDataModule).GetCustomAttribute<EmberModuleAttribute>().Enabled, Is.False);
        }

        [Test]
        public void OnInitSelfLoadsRealDefaultAssetAndOnlyPreparesIndex()
        {
            _module.UseDefaultConfigurationForNextInit(_root);
            Ready();
            var config = Resources.Load<PlayerDataConfig>(PlayerDataModule.CONFIG_PATH);
            Assert.That(config, Is.Not.Null);
            var options = config.CreateOptions(_root);
            Assert.That(_module.GetSlots().Slots.Select(slot => slot.SlotId), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(_module.LastSuccessfulSaveSlot, Is.Null);
            Assert.That(Directory.GetFiles(options.RootDirectory).Select(Path.GetFileName), Is.EquivalentTo(new[] { ".writer.lock" }));
            Assert.That(Read().Status, Is.EqualTo(ReadStatus.NotFound));
            Assert.That(Await(Save(7, 2).Completion).Succeeded, Is.True);
            Assert.That(Read(2).Progress.coins, Is.EqualTo(7));
        }

        [TestCase("missing")]
        [TestCase("traversal")]
        [TestCase("duplicates")]
        [TestCase("capacity")]
        public void InvalidConfigCompletesReadinessWithoutDefaultFallback(string kind)
        {
            if (kind == "missing") _module.SetConfigurationForNextInit(null, _root);
            if (kind == "traversal") _config.Configure("../escape", _codec.DataTypeId, new[] { 0 });
            if (kind == "duplicates") _config.Configure("Test", _codec.DataTypeId, new[] { 0, 0 });
            if (kind == "capacity") _config.Configure("Test", _codec.DataTypeId, new[] { 0 }, queueCapacity: 0);
            _lifecycle.OnInit();
            Assert.That(Await(_module.WhenReady).Status, Is.EqualTo(PrepareStatus.ConfigInvalid));
            Assert.That(_module.State, Is.EqualTo(SaveServiceState.Faulted));
            Assert.That(Save(2).Completion.Result.Status, Is.EqualTo(SaveStatus.NotReady));
            Assert.That(Directory.Exists(_root), Is.False);
        }

        [Test]
        public void PrepareFailurePreservesBrokenIndexAndDoesNotCreateProgress()
        {
            string root = _config.CreateOptions(_root).RootDirectory;
            Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "index.json"), "broken");
            _lifecycle.OnInit();
            Assert.That(Await(_module.WhenReady).Status, Is.EqualTo(PrepareStatus.IndexCorrupt));
            _lifecycle.OnDestroy(); Await(_module.WhenStopped);
            Assert.That(File.ReadAllText(Path.Combine(root, "index.json")), Is.EqualTo("broken"));
            Assert.That(Directory.GetFiles(root, "*.progress"), Is.Empty);
        }

        [Test]
        public void ExplicitSnapshotAndIndependentReadsReplaceCachedSaveBehavior()
        {
            Ready();
            var snapshot = new ProgressModel { level = 5, coins = 20 };
            var save = _module.SaveAsync(1, snapshot, new SaveSummary("checkpoint"), _codec);
            snapshot.coins = 99;
            Assert.That(Await(save.Completion).Succeeded, Is.True);
            var read = Read(1); Assert.That(read.Progress.coins, Is.EqualTo(20));
            read.Progress.coins = 88;
            Assert.That(Read(1).Progress.coins, Is.EqualTo(20));
            _lifecycle.OnDestroy(); Await(_module.WhenStopped);
            _lifecycle.ResetModuleData(); Ready();
            Assert.That(Read(1).Progress.coins, Is.EqualTo(20));
            Assert.That(_module.GetSlots().Revision, Is.EqualTo(1));
            Assert.That(_module.LastSuccessfulSaveSlot, Is.EqualTo(1));
        }

        [Test]
        public void DestroyDoesNotCreateSaveFromDefaultsAndNewGameObjectsAreIndependent()
        {
            var first = new ProgressModel(); var second = new ProgressModel();
            first.coins = 50; Assert.That(second.coins, Is.Zero);
            Ready(); _lifecycle.OnDestroy(); Await(_module.WhenStopped);
            Assert.That(Directory.GetFiles(_root, "*.progress", SearchOption.AllDirectories), Is.Empty);
            Assert.That(Directory.GetFiles(_root, "index.json", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void ActiveConfigIsFrozenAndInjectionCannotHotReplaceIt()
        {
            Ready();
            _config.Configure("Changed", _codec.DataTypeId, new[] { 9 });
            Assert.Throws<InvalidOperationException>(() => _module.SetConfigurationForNextInit(_config, _root));
            Assert.That(Await(Save(3, 1).Completion).Succeeded, Is.True);
            Assert.That(Save(4, 9).Completion.Result.Status, Is.EqualTo(SaveStatus.InvalidSlot));
            Assert.That(Directory.Exists(Path.Combine(_root, "EmberProgress", "Changed")), Is.False);
        }

        [Test]
        public void StopDuringPrepareSettlesOldReadinessAndLateCompletionCannotReviveIt()
        {
            _files.Release.Reset(); _files.BlockPrepare = 1;
            _lifecycle.OnInit(); var oldReady = _module.WhenReady; long oldEpoch = _module.ServiceEpoch;
            WaitUntil(() => _files.Entered.IsSet);
            _lifecycle.OnDestroy();
            Assert.That(oldReady.Result.Status, Is.EqualTo(PrepareStatus.Stopped));
            _lifecycle.ResetModuleData(); _lifecycle.OnInit();
            var currentReady = _module.WhenReady;
            Assert.That(currentReady.IsCompleted, Is.False);
            Assert.That(_module.State, Is.EqualTo(SaveServiceState.Preparing));
            _files.Release.Set();
            Assert.That(Await(currentReady).Succeeded, Is.True);
            Assert.That(_module.ServiceEpoch, Is.GreaterThan(oldEpoch));
            Assert.That(oldReady.Result.Status, Is.EqualTo(PrepareStatus.Stopped));
        }

        [Test]
        public void ReentryWaitsForOldWriterAndRetainsItsUnacknowledgedCancelledResult()
        {
            Ready(); _files.Release.Reset(); _files.BlockSave = 1;
            var old = Save(1); WaitUntil(() => _files.Entered.IsSet);
            _lifecycle.OnDestroy(); var stopped = _module.WhenStopped;
            _config.Configure("NextProgress", _codec.DataTypeId, new[] { 2 });
            _module.SetConfigurationForNextInit(_config, _root);
            _lifecycle.ResetModuleData(); _lifecycle.OnInit();
            // 下一世代在等待期间也已经冻结配置，Reset 不擦除已注入的下一次配置。
            _config.Configure("TooLate", _codec.DataTypeId, new[] { 3 });
            Assert.That(stopped.IsCompleted, Is.False);
            Assert.That(_module.WhenReady.IsCompleted, Is.False);
            _files.Release.Set();
            Assert.That(Await(_module.WhenReady).Succeeded, Is.True);
            Assert.That(old.Completion.Result.Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(Await(Save(9, 2).Completion).Succeeded, Is.True);
            var ticket = _module.BeginDrain();
            var result = Await(_module.DrainAsync(ticket));
            Assert.That(result.Status, Is.EqualTo(DrainStatus.Failed));
            Assert.That(result.FailedWrites.Single().Context.ServiceEpoch, Is.EqualTo(old.Context.ServiceEpoch));
            _module.AcknowledgeResults(result.Operations.Select(item => item.Context.RequestId).ToArray());
            Assert.That(_module.ResumeRequests(ticket), Is.True);
            Assert.That(Await(_module.DrainAsync(_module.BeginDrain())).Status, Is.EqualTo(DrainStatus.Drained));
        }

        [Test]
        public void FailureSurvivesResetUntilAcknowledgedAndOldTicketCannotResumeNewService()
        {
            Ready(); _files.FailSave = true;
            var failed = Save(1); Assert.That(Await(failed.Completion).Status, Is.EqualTo(SaveStatus.WriteFailed));
            var oldTicket = _module.BeginDrain();
            _lifecycle.OnDestroy(); Await(_module.WhenStopped);
            _files.FailSave = false;
            _lifecycle.ResetModuleData(); Ready();
            Assert.That(_module.ResumeRequests(oldTicket), Is.False);
            var current = _module.BeginDrain();
            Assert.That(Await(_module.DrainAsync(current)).FailedWrites.Single().Context.RequestId, Is.EqualTo(failed.Context.RequestId));
            _module.AcknowledgeResults(new[] { failed.Context.RequestId });
            Assert.That(_module.ResumeRequests(current), Is.True);
            Assert.That(Await(_module.DrainAsync(_module.BeginDrain())).Status, Is.EqualTo(DrainStatus.Drained));
        }

        [Test]
        public void CodecRejectsMissingFieldsBadTypesAndVersionMismatchInsteadOfDefaulting()
        {
            foreach (string json in new[] { "{}", "{\"version\":1,\"level\":1}", "{\"version\":2,\"level\":1,\"coins\":0}",
                "{\"version\":1,\"level\":\"one\",\"coins\":0}", "not json" })
                Assert.That(_codec.Decode(Encoding.UTF8.GetBytes(json), 1).Status, Is.EqualTo(DecodeStatus.DecodeFailed));
            Assert.That(_codec.Decode(Array.Empty<byte>(), 9).Status, Is.EqualTo(DecodeStatus.DataVersionUnsupported));
        }

        [Test]
        public void OldImplicitApiIsRemovedAndExplicitNormalExitWaitsForDrain()
        {
            foreach (string member in new[] { "Data", "CurrentSlot", "HasLoaded", "Load", "Save", "SwitchSlot", "ResetData", "DeleteSave" })
                Assert.That(typeof(PlayerDataModule).GetMember(member), Is.Empty);
            Ready(); var save = Save(7);
            var result = Await(_module.DrainAsync(_module.BeginDrain()));
            Assert.That(save.Completion.Result.Succeeded, Is.True);
            Assert.That(result.Status, Is.EqualTo(DrainStatus.Drained));
            _module.AcknowledgeResults(result.Operations.Select(item => item.Context.RequestId).ToArray());
            Await(_module.StopAsync());
            Assert.That(_module.GetUnacknowledgedSaveResults(), Is.Empty);
        }
        #endregion
    }
}
