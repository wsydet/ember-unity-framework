using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ember.Core;
using Game.Module;
using NUnit.Framework;
using UnityEngine.TestTools;
using ProgressModel = Game.Module.PlayerData;

namespace Game.PlayerData.Tests
{
    /// <summary>使用 Unity 自身的线程上下文，验证异步完成能回到 Editor 主线程。</summary>
    public sealed class PlayerDataUnityContextTests
    {
        #region 内部参数
        private PlayerDataModule _module;
        private string _root;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private IEnumerator Wait(Task task)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!task.IsCompleted)
            {
                if (DateTime.UtcNow > deadline) Assert.Fail("Unity context continuation timed out.");
                yield return null;
            }
            task.GetAwaiter().GetResult();
        }

        private sealed class ObservedCodec : IProgressCodec<ProgressModel>
        {
            private readonly PlayerDataCodec _codec = new();
            internal int EncodeThread;
            internal int DecodeThread;
            public string DataTypeId => _codec.DataTypeId;
            public EncodedProgress Encode(ProgressModel progress)
            { EncodeThread = Thread.CurrentThread.ManagedThreadId; return _codec.Encode(progress); }
            public DecodeResult<ProgressModel> Decode(ReadOnlyMemory<byte> bytes, int version)
            { DecodeThread = Thread.CurrentThread.ManagedThreadId; return _codec.Decode(bytes, version); }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [UnityTest]
        public IEnumerator UnityMainThreadCanPrepareSaveReadDrainAndStopWithoutBlocking()
        {
            Assert.That(SynchronizationContext.Current, Is.Not.Null);
            int owner = Thread.CurrentThread.ManagedThreadId;
            _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberUnitySaveTests", Guid.NewGuid().ToString("N")));
            _module = new PlayerDataModule();
            _module.UseDefaultConfigurationForNextInit(_root);
            ((IEmberModule)_module).OnInit();
            yield return Wait(_module.WhenReady);
            Assert.That(_module.WhenReady.Result.Succeeded, Is.True);
            var codec = new ObservedCodec();
            var saved = _module.SaveAsync(0, new ProgressModel { level = 4, coins = 12 }, new SaveSummary("unity"), codec);
            yield return Wait(saved.Completion);
            Assert.That(saved.Completion.Result.Succeeded, Is.True);
            var loaded = _module.ReadAsync(0, codec);
            yield return Wait(loaded.Completion);
            Assert.That(loaded.Completion.Result.Progress.coins, Is.EqualTo(12));
            Assert.That(codec.EncodeThread, Is.EqualTo(owner));
            Assert.That(codec.DecodeThread, Is.EqualTo(owner));
            var drain = _module.DrainAsync(_module.BeginDrain());
            yield return Wait(drain);
            Assert.That(drain.Result.Status, Is.EqualTo(DrainStatus.Drained));
            yield return Wait(_module.StopAsync());
            Assert.That(_module.State, Is.EqualTo(SaveServiceState.Stopped));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_module != null) yield return Wait(_module.StopAsync());
            if (_root == null) yield break;
            string boundary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberUnitySaveTests")) + Path.DirectorySeparatorChar;
            Assert.That(_root.StartsWith(boundary, StringComparison.OrdinalIgnoreCase), Is.True);
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        #endregion
    }
}
