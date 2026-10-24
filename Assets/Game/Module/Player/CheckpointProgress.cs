using System;
using System.IO;
using System.Text;
using Game.Entity;
using Game.PlayerData;

namespace Game.Player
{
    public sealed class CheckpointProgress<T>
    {
        #region 内部参数

        public string RunId { get; }
        public string LevelId { get; }
        public PlayerCheckpointRecord Checkpoint { get; }
        public T Project { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public CheckpointProgress(string runId, string levelId, PlayerCheckpointRecord checkpoint, T project)
        {
            if (string.IsNullOrWhiteSpace(runId) || runId.Length > 256 || string.IsNullOrWhiteSpace(levelId) || levelId.Length > 256)
                throw new ArgumentException("组合进度需要有限长度的轮次和关卡ID。");
            RunId = runId; LevelId = levelId; Checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint)); Project = project;
        }

        #endregion
    }

    /// <summary>合法检查点、世界记录和最新项目进度编码为同一条存储记录；不采集瞬时玩家动作。</summary>
    public sealed class CheckpointProgressCodec<T> : IProgressCodec<CheckpointProgress<T>>
    {
        #region 内部参数

        private const int Version = 1, MaxBytes = 4 * 1024 * 1024;
        private readonly IProgressCodec<T> _project;
        private readonly PlatformerSnapshotCodec _player = new();
        public string DataTypeId { get; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void Write(BinaryWriter writer, EncodedProgress data)
        {
            if (data?.Payload == null || data.Payload.Length > MaxBytes) throw new InvalidDataException("组合子记录过大或缺失。");
            writer.Write(data.DataVersion); writer.Write(data.Payload.Length); writer.Write(data.Payload);
        }
        private static EncodedProgress Read(BinaryReader reader)
        {
            int version = reader.ReadInt32(), length = reader.ReadInt32();
            if (length < 0 || length > MaxBytes || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("组合子记录长度无效。");
            return new EncodedProgress(reader.ReadBytes(length), version);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public CheckpointProgressCodec(string dataTypeId, IProgressCodec<T> project)
        {
            if (string.IsNullOrWhiteSpace(dataTypeId)) throw new ArgumentException("组合存档需要项目数据类型ID。");
            DataTypeId = dataTypeId; _project = project ?? throw new ArgumentNullException(nameof(project));
        }
        public EncodedProgress Encode(CheckpointProgress<T> snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true);
            writer.Write(Version); writer.Write(snapshot.RunId); writer.Write(snapshot.LevelId); writer.Write(snapshot.Checkpoint.CheckpointId);
            Write(writer, _player.Encode(snapshot.Checkpoint.Player));
            var world = snapshot.Checkpoint.CopyWorld();
            if (world.Length > 1024) throw new InvalidDataException("世界记录过多。");
            writer.Write(world.Length);
            foreach (var item in world) { writer.Write(item.Id); writer.Write(item.State); }
            writer.Write(_project.DataTypeId); Write(writer, _project.Encode(snapshot.Project)); writer.Flush();
            if (stream.Length > MaxBytes) throw new InvalidDataException("组合记录超过大小限制。");
            return new EncodedProgress(stream.ToArray(), Version);
        }
        public DecodeResult<CheckpointProgress<T>> Decode(ReadOnlyMemory<byte> payload, int dataVersion)
        {
            if (dataVersion != Version) return new(DecodeStatus.DataVersionUnsupported);
            try
            {
                if (payload.Length > MaxBytes) throw new InvalidDataException("组合记录过大。");
                using var stream = new MemoryStream(payload.ToArray(), false);
                using var reader = new BinaryReader(stream, new UTF8Encoding(false, true));
                if (reader.ReadInt32() != Version) throw new InvalidDataException("组合版本不一致。");
                string run = reader.ReadString(), level = reader.ReadString(), checkpoint = reader.ReadString();
                var playerBytes = Read(reader); var player = _player.Decode(playerBytes.Payload, playerBytes.DataVersion);
                if (player.Status != DecodeStatus.Decoded) throw new InvalidDataException(player.Error ?? "玩家基线解码失败。");
                int count = reader.ReadInt32();
                if (count < 0 || count > 1024) throw new InvalidDataException("世界记录数量无效。");
                var world = new WorldStateRecord2D[count]; var ids = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < count; i++)
                {
                    world[i] = new WorldStateRecord2D(reader.ReadString(), reader.ReadString());
                    if (!ids.Add(world[i].Id)) throw new InvalidDataException("世界稳定ID重复。");
                }
                if (reader.ReadString() != _project.DataTypeId) throw new InvalidDataException("项目数据类型不匹配。");
                var projectBytes = Read(reader); var project = _project.Decode(projectBytes.Payload, projectBytes.DataVersion);
                if (project.Status != DecodeStatus.Decoded || stream.Position != stream.Length) throw new InvalidDataException(project.Error ?? "项目数据或尾部无效。");
                return new(DecodeStatus.Decoded, new CheckpointProgress<T>(run, level, new PlayerCheckpointRecord(checkpoint, player.Progress, world), project.Progress));
            }
            catch (Exception error) { return new(DecodeStatus.DecodeFailed, error: error.Message); }
        }

        #endregion
    }
}
