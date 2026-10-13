using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Ember.Basic.LitJson;
using NUnit.Framework;

namespace Game.PlayerData.Tests
{
    public sealed class ProgressFileStoreTests
    {
        #region 内部参数
        private string _root;
        private ProgressStoreOptions _options;
        private ProgressFileStore _store;
        private FaultFiles _files;
        private SampleCodec _codec;
        private long _request;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private ProgressRequestContext Request(int? slot = null) => new(++_request, slot, 7);

        private FrozenProgress Freeze(string value, int slot = 0, int version = 1)
        {
            _codec.Version = version;
            var error = ProgressSnapshot.TryFreeze(Request(slot), _options, new SampleProgress { Value = value },
                new SaveSummary(value, "说明", new Dictionary<string, string> { ["checkpoint"] = value }), _codec, out var frozen);
            Assert.That(error, Is.Null);
            return frozen;
        }

        private SaveResult Save(string value, int slot = 0)
        {
            var result = _store.Save(Request(slot), Freeze(value, slot));
            Assert.That(result.Status, Is.EqualTo(SaveStatus.Committed), result.Error);
            return result;
        }

        private ReadResult<SampleProgress> Read(int slot = 0) =>
            ProgressSnapshot.Decode(_store.Read(Request(slot)), _options.DataTypeId, _codec);

        private PrepareResult Reopen(ProgressStoreOptions options = null)
        {
            _store.Dispose();
            _store = new ProgressFileStore(options ?? _options);
            return _store.Prepare(Request());
        }

        private void RewriteIndex(Action<JsonData> change, bool updateHash = true)
        {
            string path = Path.Combine(_root, "index.json");
            var envelope = JsonMapper.ToObject(File.ReadAllText(path));
            var body = JsonMapper.ToObject((string)envelope["body"]);
            change(body);
            string json = body.ToJson();
            envelope["body"] = json;
            if (updateHash) envelope["bodySha256"] = ProgressFileFormat.Hash(Encoding.UTF8.GetBytes(json));
            File.WriteAllText(path, envelope.ToJson(), new UTF8Encoding(false));
        }

        private sealed class SampleProgress { internal string Value; }

        private sealed class SampleCodec : IProgressCodec<SampleProgress>
        {
            internal int Version = 1;
            internal string Family = "test.complete-progress";
            internal byte[] LastEncoded;
            internal int EncodeCount;
            internal int DecodeCount;
            internal bool ThrowEncode;
            internal bool ThrowDecode;
            public string DataTypeId => Family;

            public EncodedProgress Encode(SampleProgress snapshot)
            {
                EncodeCount++;
                if (ThrowEncode) throw new InvalidOperationException("injected codec failure");
                LastEncoded = Encoding.UTF8.GetBytes(snapshot.Value);
                return new EncodedProgress(LastEncoded, Version);
            }

            public DecodeResult<SampleProgress> Decode(ReadOnlyMemory<byte> payload, int dataVersion)
            {
                DecodeCount++;
                if (ThrowDecode) throw new InvalidOperationException("injected codec failure");
                if (dataVersion != 1) return new DecodeResult<SampleProgress>(DecodeStatus.DataVersionUnsupported);
                return new DecodeResult<SampleProgress>(DecodeStatus.Decoded,
                    new SampleProgress { Value = Encoding.UTF8.GetString(payload.ToArray()) });
            }
        }

        private sealed class FaultFiles : ProgressFileIO
        {
            internal Action<string> AfterWrite;
            internal Action BeforeCommit;
            internal bool FailIndexWrite;
            internal bool FailBeforeCommit;
            internal bool FailAfterCommit;
            internal bool FailReadback;
            internal bool HasCommitted;

            internal override void WriteNewAndFlush(string path, byte[] bytes)
            {
                if (FailIndexWrite && path.EndsWith(".index.tmp", StringComparison.Ordinal))
                    throw new IOException("injected index write failure");
                base.WriteNewAndFlush(path, bytes);
                AfterWrite?.Invoke(path);
            }

            internal override void MoveNew(string source, string destination)
            {
                if (!destination.EndsWith("index.json", StringComparison.Ordinal))
                { base.MoveNew(source, destination); return; }
                BeforeCommit?.Invoke();
                if (FailBeforeCommit) throw new IOException("injected pre-commit failure");
                base.MoveNew(source, destination);
                HasCommitted = true;
                if (FailAfterCommit) throw new IOException("injected post-commit failure");
            }

            internal override void ReplaceWithBackup(string source, string destination, string backup)
            {
                BeforeCommit?.Invoke();
                if (FailBeforeCommit) throw new IOException("injected pre-commit failure");
                base.ReplaceWithBackup(source, destination, backup);
                HasCommitted = true;
                if (FailAfterCommit) throw new IOException("injected post-commit failure");
            }

            internal override byte[] ReadBounded(string path, int maximumBytes)
            {
                if (FailReadback && HasCommitted && path.EndsWith("index.json", StringComparison.Ordinal))
                    throw new IOException("injected inaccessible commit result");
                return base.ReadBounded(path, maximumBytes);
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberProgressTests", Guid.NewGuid().ToString("N")));
            _options = new ProgressStoreOptions(_root, "test.complete-progress", new[] { 0, 1 });
            _files = new FaultFiles();
            _codec = new SampleCodec();
            _request = 0;
            _store = new ProgressFileStore(_options, _files);
            Assert.That(_store.Prepare(Request()).Status, Is.EqualTo(PrepareStatus.Ready));
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
            string boundary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberProgressTests")) + Path.DirectorySeparatorChar;
            Assert.That(_root.StartsWith(boundary, StringComparison.OrdinalIgnoreCase), Is.True);
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void PrepareOnlyBuildsEmptyCatalogAndDoesNotReadOrCreateProgress()
        {
            Assert.That(_store.Catalog.Slots.Count, Is.EqualTo(2));
            Assert.That(_store.Catalog.Slots.All(slot => !slot.HasProgress), Is.True);
            Assert.That(_store.Catalog.LastSuccessfulSaveSlot, Is.Null);
            Assert.That(Directory.GetFiles(_root).Select(Path.GetFileName), Is.EquivalentTo(new[] { ".writer.lock" }));
            Assert.That(Read().Status, Is.EqualTo(ReadStatus.NotFound));
            Assert.That(_codec.DecodeCount, Is.Zero);
        }

        [Test]
        public void FrozenPayloadAndSummarySurviveCallerMutations()
        {
            var source = new SampleProgress { Value = "original" };
            var metadata = new Dictionary<string, string> { ["checkpoint"] = "before" };
            var summary = new SaveSummary("title", "description", metadata);
            Assert.That(ProgressSnapshot.TryFreeze(Request(0), _options, source, summary, _codec, out var frozen), Is.Null);
            source.Value = "changed";
            Array.Clear(_codec.LastEncoded, 0, _codec.LastEncoded.Length);
            metadata["checkpoint"] = "after";
            Assert.That(_store.Save(Request(0), frozen).Status, Is.EqualTo(SaveStatus.Committed));
            Assert.That(Reopen().Succeeded, Is.True);
            Assert.That(Read().Progress.Value, Is.EqualTo("original"));
            Assert.That(_store.Catalog.Slots[0].Summary.Metadata["checkpoint"], Is.EqualTo("before"));
        }

        [Test]
        public void RoundTripKeepsOtherSlotsAndLatestSaveAcrossReopen()
        {
            Save("zero", 0);
            Save("one", 1);
            Save("zero revised", 0);
            Assert.That(Reopen().Succeeded, Is.True);
            var result = Read(1);
            Assert.That(result.Progress.Value, Is.EqualTo("one"));
            Assert.That(result.Context.SlotId, Is.EqualTo(1));
            Assert.That(result.Context.ServiceEpoch, Is.EqualTo(7));
            Assert.That(result.Context.RequestId, Is.GreaterThan(0));
            Assert.That(Read(0).Progress.Value, Is.EqualTo("zero revised"));
            Assert.That(_store.Catalog.Revision, Is.EqualTo(3));
            Assert.That(_store.Catalog.LastSuccessfulSaveSlot, Is.EqualTo(0));
        }

        [Test]
        public void EachDecodeReturnsIndependentProgressAndCatalogSnapshotsDoNotChange()
        {
            var empty = _store.Catalog;
            Save("first");
            var firstCatalog = _store.Catalog;
            var first = Read();
            first.Progress.Value = "local mutation";
            Assert.That(Read().Progress.Value, Is.EqualTo("first"));
            Save("second", 1);
            Assert.That(empty.Revision, Is.Zero);
            Assert.That(empty.Slots.All(slot => !slot.HasProgress), Is.True);
            Assert.That(firstCatalog.Revision, Is.EqualTo(1));
            Assert.That(firstCatalog.LastSuccessfulSaveSlot, Is.EqualTo(0));
            Assert.That(firstCatalog.Slots[1].HasProgress, Is.False);
        }

        [Test]
        public void BackupAndEveryReferencedPayloadAreRetained()
        {
            Save("first");
            Save("second");
            Save("third");
            var backup = ProgressFileFormat.DecodeIndex(File.ReadAllBytes(Path.Combine(_root, "index.json.bak")), _options);
            Assert.That(backup.Revision, Is.EqualTo(2));
            foreach (var entry in backup.Entries.Values)
            {
                var bytes = File.ReadAllBytes(Path.Combine(_root, entry.FileName));
                Assert.That(ProgressFileFormat.Hash(bytes), Is.EqualTo(entry.Sha256));
            }
            Assert.That(Directory.GetFiles(_root, "*.progress").Length, Is.EqualTo(3));
            Assert.That(Directory.GetFiles(_root, "*.tmp"), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IndexFailurePreservesCommittedCatalogAndReadableOldSave(bool failInsideCommit)
        {
            Save("old");
            var bytes = File.ReadAllBytes(Path.Combine(_root, "index.json"));
            _files.FailIndexWrite = !failInsideCommit;
            _files.FailBeforeCommit = failInsideCommit;
            var result = _store.Save(Request(1), Freeze("candidate", 1));
            Assert.That(result.Status, Is.EqualTo(SaveStatus.WriteFailed), result.Error);
            Assert.That(File.ReadAllBytes(Path.Combine(_root, "index.json")), Is.EqualTo(bytes));
            Assert.That(Read(0).Progress.Value, Is.EqualTo("old"));
            Assert.That(Read(1).Status, Is.EqualTo(ReadStatus.NotFound));
            Assert.That(_store.Catalog.LastSuccessfulSaveSlot, Is.EqualTo(0));
            Assert.That(_store.Catalog.Revision, Is.EqualTo(1));
            Assert.That(Reopen().Succeeded, Is.True);
            Assert.That(Read(0).Progress.Value, Is.EqualTo("old"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExceptionAfterActualCommitReturnsCommittedByReadback(bool existingIndex)
        {
            if (existingIndex) Save("old");
            _files.FailAfterCommit = true;
            var result = _store.Save(Request(1), Freeze("committed", 1));
            Assert.That(result.Status, Is.EqualTo(SaveStatus.Committed), result.Error);
            Assert.That(_store.Catalog.LastSuccessfulSaveSlot, Is.EqualTo(1));
            Assert.That(Reopen().Succeeded, Is.True);
            Assert.That(Read(1).Progress.Value, Is.EqualTo("committed"));
        }

        [Test]
        public void UncertainCommitStopsFurtherWritesUntilOwnershipIsReleasedAndDiskRechecked()
        {
            Save("old");
            _files.FailAfterCommit = true;
            _files.FailReadback = true;
            var result = _store.Save(Request(1), Freeze("actually committed", 1));
            Assert.That(result.Status, Is.EqualTo(SaveStatus.CommitUncertain));
            Assert.That(result.CommitId, Is.Not.Null);
            Assert.That(_store.Catalog.State, Is.EqualTo(SaveServiceState.Faulted));
            Assert.That(_store.Catalog.Revision, Is.EqualTo(1));
            Assert.That(_store.Save(Request(0), Freeze("must not write")).Status, Is.EqualTo(SaveStatus.NotReady));
            using (var second = new ProgressFileStore(_options))
                Assert.That(second.Prepare(Request()).Status, Is.EqualTo(PrepareStatus.IoFailed));
            Assert.That(Reopen().Succeeded, Is.True);
            Assert.That(_store.Catalog.Revision, Is.EqualTo(2));
            Assert.That(Read(1).Progress.Value, Is.EqualTo("actually committed"));
        }

        [TestCase("before")]
        [TestCase("payload")]
        [TestCase("index")]
        public void CancellationBeforeCommitNeverPublishesCandidate(string point)
        {
            Save("old");
            using var cancellation = new CancellationTokenSource();
            if (point == "before") cancellation.Cancel();
            _files.AfterWrite = path =>
            {
                if (point == "payload" && path.EndsWith(".progress.tmp", StringComparison.Ordinal)
                    || point == "index" && path.EndsWith(".index.tmp", StringComparison.Ordinal)) cancellation.Cancel();
            };
            var result = _store.Save(Request(1), Freeze("cancelled", 1), cancellation.Token);
            Assert.That(result.Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(_store.Catalog.Revision, Is.EqualTo(1));
            Assert.That(Read().Progress.Value, Is.EqualTo("old"));
            Assert.That(Read(1).Status, Is.EqualTo(ReadStatus.NotFound));
            Assert.That(Directory.GetFiles(_root, "*.tmp"), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellationInsideCommitReturnsActualResult(bool injectFailure)
        {
            Save("old");
            using var cancellation = new CancellationTokenSource();
            _files.BeforeCommit = cancellation.Cancel;
            _files.FailBeforeCommit = injectFailure;
            var result = _store.Save(Request(1), Freeze("new", 1), cancellation.Token);
            Assert.That(result.Status, Is.EqualTo(injectFailure ? SaveStatus.WriteFailed : SaveStatus.Committed));
            Assert.That(_store.Catalog.LastSuccessfulSaveSlot, Is.EqualTo(injectFailure ? 0 : 1));
        }

        [Test]
        public void CancelledReadDoesNotDecodeOrChangeLatest()
        {
            Save("old");
            var context = Request(0);
            var result = ProgressSnapshot.Decode(_store.Read(context, new CancellationToken(true)), _options.DataTypeId, _codec);
            Assert.That(result.Status, Is.EqualTo(ReadStatus.Cancelled));
            Assert.That(result.Context.RequestId, Is.EqualTo(context.RequestId));
            Assert.That(_codec.DecodeCount, Is.Zero);
            Assert.That(_store.Catalog.LastSuccessfulSaveSlot, Is.EqualTo(0));
        }

        [TestCase("modify")]
        [TestCase("remove")]
        [TestCase("oversize")]
        public void BrokenPayloadIsReportedWithoutDefaultsOrRewriting(string damage)
        {
            Save("old");
            var path = Directory.GetFiles(_root, "*.progress").Single();
            var index = File.ReadAllBytes(Path.Combine(_root, "index.json"));
            if (damage == "remove") File.Delete(path);
            else if (damage == "oversize") File.WriteAllBytes(path, new byte[_options.MaxPayloadBytes + 1025]);
            else File.WriteAllText(path, "broken");
            var result = Read();
            Assert.That(result.Status, Is.EqualTo(ReadStatus.IntegrityFailed));
            Assert.That(result.Progress, Is.Null);
            Assert.That(_codec.DecodeCount, Is.Zero);
            Assert.That(File.ReadAllBytes(Path.Combine(_root, "index.json")), Is.EqualTo(index));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CorruptOrMissingMainIndexNeverSilentlyUsesBackup(bool missing)
        {
            Save("old"); Save("new");
            var backup = File.ReadAllBytes(Path.Combine(_root, "index.json.bak"));
            string path = Path.Combine(_root, "index.json");
            if (missing) File.Delete(path);
            else RewriteIndex(body => body["slots"][0]["summary"]["title"] = "changed", false);
            Assert.That(Reopen().Status, Is.EqualTo(PrepareStatus.IndexCorrupt));
            Assert.That(_store.Catalog.State, Is.EqualTo(SaveServiceState.Faulted));
            Assert.That(File.ReadAllBytes(Path.Combine(_root, "index.json.bak")), Is.EqualTo(backup));
            Assert.That(File.Exists(path), Is.EqualTo(!missing));
        }

        [Test]
        public void PathTraversalIsRejectedEvenWithValidIndexChecksum()
        {
            Save("old");
            RewriteIndex(body => body["slots"][0]["file"] = "../outside.progress");
            Assert.That(Reopen().Status, Is.EqualTo(PrepareStatus.IndexCorrupt));
        }

        [Test]
        public void UnsupportedStorageVersionIsDistinctFromUnsupportedBusinessVersion()
        {
            Assert.That(_store.Save(Request(0), Freeze("future", version: 9)).Succeeded, Is.True);
            Assert.That(Read().Status, Is.EqualTo(ReadStatus.DataVersionUnsupported));
            string path = Path.Combine(_root, "index.json");
            var index = JsonMapper.ToObject(File.ReadAllText(path));
            index["formatVersion"] = 900;
            File.WriteAllText(path, index.ToJson());
            var before = File.ReadAllBytes(path);
            Assert.That(Reopen().Status, Is.EqualTo(PrepareStatus.StorageVersionUnsupported));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ChangedFamilyOrRemovedConfiguredSlotFailsPreparation(bool differentFamily)
        {
            Save("old");
            var options = new ProgressStoreOptions(_root,
                differentFamily ? "other.family" : _options.DataTypeId, differentFamily ? new[] { 0, 1 } : new[] { 1 });
            Assert.That(Reopen(options).Status, Is.EqualTo(PrepareStatus.ConfigInvalid));
        }

        [Test]
        public void RejectInvalidSlotAndWrongCodecBeforeEncoding()
        {
            var input = new SampleProgress { Value = "unused" };
            Assert.That(ProgressSnapshot.TryFreeze(Request(99), _options, input, new SaveSummary(), _codec, out _).Status,
                Is.EqualTo(SaveStatus.InvalidSlot));
            _codec.Family = "other.family";
            Assert.That(ProgressSnapshot.TryFreeze(Request(0), _options, input, new SaveSummary(), _codec, out _).Status,
                Is.EqualTo(SaveStatus.WrongDataType));
            Assert.That(_codec.EncodeCount, Is.Zero);
            Assert.That(Directory.GetFiles(_root, "*.progress"), Is.Empty);
        }

        [Test]
        public void CodecExceptionsAndOversizeSnapshotsAreExplicitFailures()
        {
            var input = new SampleProgress { Value = "12345" };
            _codec.ThrowEncode = true;
            Assert.That(ProgressSnapshot.TryFreeze(Request(0), _options, input, new SaveSummary(), _codec, out _).Status,
                Is.EqualTo(SaveStatus.EncodeFailed));
            _codec.ThrowEncode = false;
            var options = new ProgressStoreOptions(_root, _options.DataTypeId, new[] { 0 }, maxPayloadBytes: 4);
            Assert.That(ProgressSnapshot.TryFreeze(Request(0), options, input, new SaveSummary(), _codec, out _).Status,
                Is.EqualTo(SaveStatus.TooLarge));
            Save("old");
            _codec.ThrowDecode = true;
            Assert.That(Read().Status, Is.EqualTo(ReadStatus.DecodeFailed));
            _codec.ThrowDecode = false;
            Assert.That(Read().Progress.Value, Is.EqualTo("old"));
        }

        [Test]
        public void OwnershipAndNotReadyStatesAreExplicitAndDisposeIsIdempotent()
        {
            using var other = new ProgressFileStore(_options);
            Assert.That(other.Save(Request(0), Freeze("unused")).Status, Is.EqualTo(SaveStatus.NotReady));
            Assert.That(other.Read(Request(0)).Status, Is.EqualTo(ReadStatus.NotReady));
            Assert.That(other.Prepare(Request()).Status, Is.EqualTo(PrepareStatus.IoFailed));
            _store.Dispose(); _store.Dispose();
            Assert.That(_store.Catalog.State, Is.EqualTo(SaveServiceState.Stopped));
            Assert.That(_store.Prepare(Request()).Status, Is.EqualTo(PrepareStatus.Stopped));
            using var newOwner = new ProgressFileStore(_options);
            Assert.That(newOwner.Prepare(Request()).Succeeded, Is.True);
        }
        #endregion
    }
}
