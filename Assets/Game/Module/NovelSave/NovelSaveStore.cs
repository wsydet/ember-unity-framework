using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Narrative;
using UnityEngine;

namespace Game.NovelSave
{
    [Serializable] public sealed class NovelSlot
    {
        public int Slot;
        public long SavedUtcTicks;
        public string File, Digest, Chapter, Summary;
    }
    [Serializable] public sealed class NovelSlotIndex
    {
        public int SchemaVersion = 1;
        public int Latest = -1;
        public List<NovelSlot> Slots = new();
    }
    [Serializable] public sealed class NovelAccountData
    {
        public int SchemaVersion = 1;
        public int FontSize = 1, ReadingMultiplier = 1;
        public NovelEffectPreference ShakePreference, FlashPreference;
        public List<string> ReadLines = new();
        public float TextSpeed = 32, AutoInterval = 1, BgmVolume = 1, SfxVolume = 1, VoiceVolume = 1;
    }

    /// <summary>Desktop store: immutable slot payload first, atomic index commit last. No automatic backup fallback.</summary>
    public sealed class NovelSaveStore
    {
        #region 内部参数
        private readonly string _root;
        private volatile NovelSlotIndex _index = new();
        private int _slotWriteInFlight;
        private readonly object _accountQueue = new();
        private Task<string> _accountTail = Task.FromResult<string>(null);
        public string IndexError { get; private set; }
        public IReadOnlyList<NovelSlot> Slots => _index.Slots.Select(s => new NovelSlot { Slot = s.Slot, SavedUtcTicks = s.SavedUtcTicks,
            File = s.File, Digest = s.Digest, Chapter = s.Chapter, Summary = s.Summary }).ToList();
        public int Latest => _index.Latest;
        // Test seam represents failures before filesystem commit, never a fake success result.
        public Action<string> BeforeCommit { get; set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private string Digest(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", ""); }
        private void AtomicWrite(string path, byte[] bytes)
        {
            if (bytes.Length > 4 * 1024 * 1024) throw new IOException("存档超过 4 MiB 上限，未写入");
            Directory.CreateDirectory(_root);
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                BeforeCommit?.Invoke(path);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        private byte[] ReadLimited(string path)
        {
            if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new IOException("存档文件过大");
            return File.ReadAllBytes(path);
        }
        // Called on the Unity thread. The returned operation owns only immutable bytes / plain data.
        private Func<string> PrepareSave(int slot, NovelCheckpoint checkpoint)
        {
            if (IndexError != null) throw new IOException(IndexError);
            if (slot < 0 || slot > 7 || checkpoint == null || (checkpoint.SchemaVersion < 1 || checkpoint.SchemaVersion > 7) ||
                (checkpoint.Stop != NarrativeState.AwaitingAdvance && checkpoint.Stop != NarrativeState.AwaitingChoice))
                throw new IOException("无效槽位或非稳定点快照");
            string file = "slot-" + slot + "-" + Guid.NewGuid().ToString("N") + ".json";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(checkpoint));
            var candidate = new NovelSlotIndex { Latest = slot, Slots = Slots.ToList() };
            candidate.Slots.RemoveAll(s => s.Slot == slot);
            candidate.Slots.Add(new NovelSlot { Slot = slot, File = file, Digest = Digest(bytes), SavedUtcTicks = DateTime.UtcNow.Ticks,
                Chapter = checkpoint.ChapterId, Summary = checkpoint.Stop == NarrativeState.AwaitingChoice ? "选择" : checkpoint.History.LastOrDefault()?.Text ?? checkpoint.LineId });
            byte[] indexBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(candidate));
            return () =>
            {
                try
                {
                    AtomicWrite(Path.Combine(_root, file), bytes);
                    AtomicWrite(Path.Combine(_root, "index.json"), indexBytes);
                    _index = candidate; return null;
                }
                catch (Exception ex) { return "保存失败：" + ex.Message; }
                finally { Volatile.Write(ref _slotWriteInFlight, 0); }
            };
        }
        private byte[] ReadPayload(int slot)
        {
            if (IndexError != null) throw new IOException(IndexError);
            var entry = _index.Slots.SingleOrDefault(s => s.Slot == slot);
            if (entry == null) throw new IOException("此槽位尚无存档");
            byte[] bytes = ReadLimited(Path.Combine(_root, entry.File));
            if (Digest(bytes) != entry.Digest) throw new IOException("存档校验失败；文件已保留，请选择其他槽位");
            return bytes;
        }
        private NovelCheckpoint Decode(byte[] bytes)
        {
            var checkpoint = JsonUtility.FromJson<NovelCheckpoint>(Encoding.UTF8.GetString(bytes));
            if (checkpoint == null || (checkpoint.SchemaVersion < 1 || checkpoint.SchemaVersion > 7) || string.IsNullOrWhiteSpace(checkpoint.StoryPath))
                throw new IOException("存档版本不支持或内容损坏");
            return checkpoint;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelSaveStore(string root)
        {
            _root = root;
            try
            {
                string path = Path.Combine(root, "index.json");
                if (!File.Exists(path)) return;
                var index = JsonUtility.FromJson<NovelSlotIndex>(Encoding.UTF8.GetString(ReadLimited(path)));
                if (index == null || index.SchemaVersion != 1 || index.Slots == null || index.Slots.Count > 8 ||
                    index.Slots.Any(s => s == null || s.Slot < 0 || s.Slot > 7 || s.SavedUtcTicks <= 0 || s.SavedUtcTicks > DateTime.MaxValue.Ticks ||
                        string.IsNullOrEmpty(s.File) || Path.GetFileName(s.File) != s.File || !s.File.EndsWith(".json", StringComparison.Ordinal)) ||
                    index.Slots.Select(s => s.Slot).Distinct().Count() != index.Slots.Count ||
                    (index.Slots.Count > 0 && !index.Slots.Any(s => s.Slot == index.Latest)))
                    throw new IOException("槽位索引损坏或版本不支持；原文件已保留");
                _index = index;
            }
            catch (Exception ex) { IndexError = ex.Message; }
        }
        public bool Save(int slot, NovelCheckpoint checkpoint, out string error)
        {
            if (Interlocked.CompareExchange(ref _slotWriteInFlight, 1, 0) != 0) { error = "正在写入存档"; return false; }
            try
            {
                error = PrepareSave(slot, checkpoint)(); return error == null;
            }
            catch (Exception ex) { Volatile.Write(ref _slotWriteInFlight, 0); error = "保存失败：" + ex.Message; return false; }
        }
        /// <summary>Must be started on the Unity thread. Success is reported only after both durable commits.</summary>
        public Task<string> SaveAsync(int slot, NovelCheckpoint checkpoint)
        {
            if (Interlocked.CompareExchange(ref _slotWriteInFlight, 1, 0) != 0) return Task.FromResult("正在写入存档");
            try { return Task.Run(PrepareSave(slot, checkpoint)); }
            catch (Exception ex) { Volatile.Write(ref _slotWriteInFlight, 0); return Task.FromResult("保存失败：" + ex.Message); }
        }
        /// <summary>Unity synchronization context resumes JSON decoding on the calling thread.</summary>
        public async Task<NovelCheckpoint> ReadAsync(int slot)
        {
            byte[] bytes = await Task.Run(() => ReadPayload(slot));
            return Decode(bytes);
        }
        public bool Read(int slot, out NovelCheckpoint checkpoint, out string error)
        {
            checkpoint = null; error = IndexError;
            if (error != null) return false;
            try
            {
                checkpoint = Decode(ReadPayload(slot));
                return true;
            }
            catch (Exception ex) { checkpoint = null; error = ex.Message; return false; }
        }
        public bool ReadAccount(out NovelAccountData account, out string error)
        {
            account = new(); error = null;
            try
            {
                string path = Path.Combine(_root, "account.json");
                if (!File.Exists(path)) return true;
                var loaded = JsonUtility.FromJson<NovelAccountData>(Encoding.UTF8.GetString(ReadLimited(path)));
                if (loaded == null || loaded.SchemaVersion != 1 || loaded.ReadLines == null) throw new IOException("已读/偏好文件损坏或版本不支持");
                account = loaded; return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        public bool SaveAccount(NovelAccountData account, out string error)
        {
            try { error = SaveAccountAsync(account).GetAwaiter().GetResult(); return error == null; }
            catch (Exception ex) { error = "已读/偏好保存失败：" + ex.Message; return false; }
        }
        public Task<string> SaveAccountAsync(NovelAccountData account)
        {
            // Snapshot on Unity thread; queue in order, so an older write cannot replace a newer preference/read set.
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(account));
            lock (_accountQueue)
                return _accountTail = _accountTail.ContinueWith(_ =>
                {
                    try { AtomicWrite(Path.Combine(_root, "account.json"), bytes); return null; }
                    catch (Exception ex) { return "已读/偏好保存失败：" + ex.Message; }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
        #endregion
    }
}
