using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Game.PlayerData
{
    /// <summary>通用展示摘要，不含角色、章节或其他玩法语义。构造时复制元数据。</summary>
    public sealed class SaveSummary
    {
        #region 内部参数
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public SaveSummary(string title = "", string description = "", IReadOnlyDictionary<string, string> metadata = null)
        {
            Title = title ?? "";
            Description = description ?? "";
            if (Title.Length > 256 || Description.Length > 2048 || (metadata?.Count ?? 0) > 16)
                throw new ArgumentException("摘要超过限制（标题 256、描述 2048 字符，元数据 16 项）。");
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            if (metadata != null)
                foreach (var pair in metadata)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > 64 || pair.Value == null || pair.Value.Length > 256)
                        throw new ArgumentException("摘要元数据的键/值无效（键 64、值 256 字符）。");
                    copy.Add(pair.Key, pair.Value);
                }
            Metadata = new ReadOnlyDictionary<string, string>(copy);
        }
        #endregion
    }

    public sealed class SlotMetadata
    {
        #region 内部参数
        public int SlotId { get; }
        public bool HasProgress { get; }
        public SaveSummary Summary { get; }
        public DateTime? SavedAtUtc { get; }
        public int? DataVersion { get; }
        public long? Revision { get; }
        public string CommitId { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal SlotMetadata(int slotId, SaveSummary summary = null, DateTime? savedAtUtc = null,
            int? dataVersion = null, long? revision = null, string commitId = null)
        {
            SlotId = slotId; HasProgress = commitId != null; Summary = summary;
            SavedAtUtc = savedAtUtc; DataVersion = dataVersion; Revision = revision; CommitId = commitId;
        }
        #endregion
    }

    public sealed class SlotCatalog
    {
        #region 内部参数
        public SaveServiceState State { get; }
        public long Revision { get; }
        public int? LastSuccessfulSaveSlot { get; }
        public IReadOnlyList<SlotMetadata> Slots { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal SlotCatalog(SaveServiceState state, long revision, int? latest, IList<SlotMetadata> slots)
        {
            State = state; Revision = revision; LastSuccessfulSaveSlot = latest;
            Slots = new ReadOnlyCollection<SlotMetadata>(new List<SlotMetadata>(slots));
        }
        #endregion
    }

    /// <summary>纯值存储设置；模块负责从 SO 构造，后端不调用 Unity API。</summary>
    public sealed class ProgressStoreOptions
    {
        #region 内部参数
        private readonly HashSet<int> _slotSet;
        public string RootDirectory { get; }
        public string DataTypeId { get; }
        public IReadOnlyList<int> SlotIds { get; }
        public int MaxPayloadBytes { get; }
        public int MaxIndexBytes { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public ProgressStoreOptions(string rootDirectory, string dataTypeId, IEnumerable<int> slotIds,
            int maxPayloadBytes = 4 * 1024 * 1024, int maxIndexBytes = 4 * 1024 * 1024)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Path.IsPathRooted(rootDirectory))
                throw new ArgumentException("存储根目录必须是显式绝对路径。", nameof(rootDirectory));
            if (string.IsNullOrWhiteSpace(dataTypeId) || dataTypeId.Length > 128)
                throw new ArgumentException("DataTypeId 必须非空且至多 128 字符。", nameof(dataTypeId));
            if (maxPayloadBytes <= 0 || maxPayloadBytes > 64 * 1024 * 1024 || maxIndexBytes <= 0 || maxIndexBytes > 16 * 1024 * 1024)
                throw new ArgumentOutOfRangeException(nameof(maxPayloadBytes));
            _slotSet = new HashSet<int>();
            var slots = new List<int>();
            foreach (int slot in slotIds ?? throw new ArgumentNullException(nameof(slotIds)))
            {
                if (slot < 0 || !_slotSet.Add(slot) || slots.Count >= 128)
                    throw new ArgumentException("槽位必须非负且唯一，最多 128 个。", nameof(slotIds));
                slots.Add(slot);
            }
            if (slots.Count == 0) throw new ArgumentException("至少需要一个槽位。", nameof(slotIds));
            RootDirectory = Path.GetFullPath(rootDirectory);
            DataTypeId = dataTypeId;
            SlotIds = slots.AsReadOnly();
            MaxPayloadBytes = maxPayloadBytes;
            MaxIndexBytes = maxIndexBytes;
        }

        internal bool ContainsSlot(int slot) => _slotSet.Contains(slot);
        #endregion
    }
}
