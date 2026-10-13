using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ember.Basic;
using Ember.Basic.LitJson;

namespace Game.PlayerData
{
    internal sealed class ProgressIndex
    {
        internal long Revision { get; }
        internal int? LatestSlot { get; }
        internal string CommitId { get; }
        internal Dictionary<int, ProgressEntry> Entries { get; }

        internal ProgressIndex(long revision = 0, int? latestSlot = null, string commitId = null,
            Dictionary<int, ProgressEntry> entries = null)
        {
            Revision = revision; LatestSlot = latestSlot; CommitId = commitId;
            Entries = entries ?? new Dictionary<int, ProgressEntry>();
        }
    }

    internal sealed class ProgressEntry
    {
        internal SlotMetadata Metadata { get; }
        internal string FileName { get; }
        internal int FileLength { get; }
        internal string Sha256 { get; }

        internal ProgressEntry(SlotMetadata metadata, string fileName, int fileLength, string sha256)
        { Metadata = metadata; FileName = fileName; FileLength = fileLength; Sha256 = sha256; }
    }

    internal sealed class UnsupportedProgressFormatException : Exception
    {
        internal UnsupportedProgressFormatException(int version) : base("不支持的存储格式版本：" + version) { }
    }

    internal sealed class ProgressConfigurationException : Exception
    {
        internal ProgressConfigurationException(string message) : base(message) { }
    }

    /// <summary>文件协议只处理纯数据。索引摘要覆盖业务摘要和所有引用；负载封装另带存储/业务版本。</summary>
    internal static class ProgressFileFormat
    {
        #region 内部参数
        internal const int STORAGE_VERSION = 1;
        internal const int MAX_ENVELOPE_OVERHEAD = 1024;
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("EMBERPRG");
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static long Number(JsonData value)
        {
            if (value != null && value.IsInt) return (int)value;
            if (value != null && value.IsLong) return (long)value;
            throw new InvalidDataException("预期整数。");
        }

        private static int Integer(JsonData value) => checked((int)Number(value));

        private static string Text(JsonData value)
        {
            if (value == null || !value.IsString) throw new InvalidDataException("预期字符串。");
            return (string)value;
        }

        private static bool IsHash(string text)
        {
            if (text == null || text.Length != 64) return false;
            foreach (char character in text)
                if (!(character >= '0' && character <= '9') && !(character >= 'a' && character <= 'f')) return false;
            return true;
        }

        private static string CommitId(JsonData value)
        {
            string id = Text(value);
            if (!Guid.TryParseExact(id, "N", out var parsed) || parsed.ToString("N") != id)
                throw new InvalidDataException("无效提交标识。");
            return id;
        }

        private static SaveSummary ReadSummary(JsonData data)
        {
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            var pairs = data["metadata"];
            if (pairs == null || !pairs.IsArray || pairs.Count > 16) throw new InvalidDataException("无效摘要元数据。");
            foreach (JsonData pair in pairs) metadata.Add(Text(pair["key"]), Text(pair["value"]));
            return new SaveSummary(Text(data["title"]), Text(data["description"]), metadata);
        }

        private static void WriteSummary(JsonWriter writer, SaveSummary summary)
        {
            writer.WriteObjectStart();
            writer.WritePropertyName("title"); writer.Write(summary.Title);
            writer.WritePropertyName("description"); writer.Write(summary.Description);
            writer.WritePropertyName("metadata"); writer.WriteArrayStart();
            foreach (var pair in summary.Metadata.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                writer.WriteObjectStart();
                writer.WritePropertyName("key"); writer.Write(pair.Key);
                writer.WritePropertyName("value"); writer.Write(pair.Value);
                writer.WriteObjectEnd();
            }
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return CryptographyUtils.ArrayToHexString(sha.ComputeHash(bytes));
        }

        internal static byte[] EncodePayload(FrozenProgress snapshot)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Utf8, true);
            byte[] type = Utf8.GetBytes(snapshot.DataTypeId);
            writer.Write(Magic);
            writer.Write(STORAGE_VERSION);
            writer.Write(snapshot.DataVersion);
            writer.Write(type.Length);
            writer.Write(type);
            writer.Write(snapshot.Payload.Length);
            writer.Write(snapshot.Payload);
            writer.Flush();
            return stream.ToArray();
        }

        internal static byte[] DecodePayload(byte[] bytes, ProgressStoreOptions options, int dataVersion)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8);
            if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic)) throw new InvalidDataException("负载文件标识无效。");
            int storageVersion = reader.ReadInt32();
            if (storageVersion != STORAGE_VERSION) throw new UnsupportedProgressFormatException(storageVersion);
            if (reader.ReadInt32() != dataVersion) throw new InvalidDataException("负载与索引业务版本不一致。");
            int typeLength = reader.ReadInt32();
            if (typeLength <= 0 || typeLength > 512) throw new InvalidDataException("文档族标识长度无效。");
            string type = Utf8.GetString(reader.ReadBytes(typeLength));
            if (type != options.DataTypeId) throw new InvalidDataException("负载文档族不匹配。");
            int length = reader.ReadInt32();
            if (length < 0 || length > options.MaxPayloadBytes || stream.Length - stream.Position != length)
                throw new InvalidDataException("负载大小不匹配或超过限制。");
            return reader.ReadBytes(length);
        }

        internal static byte[] EncodeIndex(ProgressIndex index, string dataTypeId)
        {
            var body = new JsonWriter();
            body.WriteObjectStart();
            body.WritePropertyName("dataTypeId"); body.Write(dataTypeId);
            body.WritePropertyName("revision"); body.Write(index.Revision);
            body.WritePropertyName("commitId"); body.Write(index.CommitId);
            body.WritePropertyName("lastSuccessfulSaveSlot"); body.Write(index.LatestSlot.Value);
            body.WritePropertyName("slots"); body.WriteArrayStart();
            foreach (var entry in index.Entries.Values.OrderBy(item => item.Metadata.SlotId))
            {
                var slot = entry.Metadata;
                body.WriteObjectStart();
                body.WritePropertyName("slotId"); body.Write(slot.SlotId);
                body.WritePropertyName("file"); body.Write(entry.FileName);
                body.WritePropertyName("fileLength"); body.Write(entry.FileLength);
                body.WritePropertyName("sha256"); body.Write(entry.Sha256);
                body.WritePropertyName("dataVersion"); body.Write(slot.DataVersion.Value);
                body.WritePropertyName("revision"); body.Write(slot.Revision.Value);
                body.WritePropertyName("commitId"); body.Write(slot.CommitId);
                body.WritePropertyName("savedUtcTicks"); body.Write(slot.SavedAtUtc.Value.Ticks);
                body.WritePropertyName("summary"); WriteSummary(body, slot.Summary);
                body.WriteObjectEnd();
            }
            body.WriteArrayEnd();
            body.WriteObjectEnd();
            string jsonBody = body.ToString();
            var envelope = new JsonWriter();
            envelope.WriteObjectStart();
            envelope.WritePropertyName("formatVersion"); envelope.Write(STORAGE_VERSION);
            envelope.WritePropertyName("body"); envelope.Write(jsonBody);
            envelope.WritePropertyName("bodySha256"); envelope.Write(Hash(Utf8.GetBytes(jsonBody)));
            envelope.WriteObjectEnd();
            return Utf8.GetBytes(envelope.ToString());
        }

        internal static ProgressIndex DecodeIndex(byte[] bytes, ProgressStoreOptions options)
        {
            if (bytes.Length > options.MaxIndexBytes) throw new InvalidDataException("索引过大。");
            var envelope = JsonMapper.ToObject(Utf8.GetString(bytes));
            int version = Integer(envelope["formatVersion"]);
            if (version != STORAGE_VERSION) throw new UnsupportedProgressFormatException(version);
            string body = Text(envelope["body"]);
            if (Hash(Utf8.GetBytes(body)) != Text(envelope["bodySha256"])) throw new InvalidDataException("索引摘要校验失败。");
            var data = JsonMapper.ToObject(body);
            if (Text(data["dataTypeId"]) != options.DataTypeId)
                throw new ProgressConfigurationException("索引属于另一文档族，请使用独立根目录。");
            long revision = Number(data["revision"]);
            int latest = Integer(data["lastSuccessfulSaveSlot"]);
            string commit = CommitId(data["commitId"]);
            var slots = data["slots"];
            if (revision <= 0 || slots == null || !slots.IsArray || slots.Count == 0 || slots.Count > 128)
                throw new InvalidDataException("索引版本或槽位集合无效。");
            var entries = new Dictionary<int, ProgressEntry>();
            foreach (JsonData slot in slots)
            {
                int id = Integer(slot["slotId"]);
                if (!options.ContainsSlot(id)) throw new ProgressConfigurationException("配置中缺少已有槽位：" + id);
                string entryCommit = CommitId(slot["commitId"]);
                string file = Text(slot["file"]);
                int fileLength = Integer(slot["fileLength"]);
                string sha = Text(slot["sha256"]);
                int dataVersion = Integer(slot["dataVersion"]);
                long savedRevision = Number(slot["revision"]);
                var savedAt = new DateTime(Number(slot["savedUtcTicks"]), DateTimeKind.Utc);
                if (file != entryCommit + ".progress" || !IsHash(sha) || dataVersion <= 0 || savedRevision <= 0
                    || savedRevision > revision || fileLength <= 0 || fileLength > options.MaxPayloadBytes + MAX_ENVELOPE_OVERHEAD)
                    throw new InvalidDataException("槽位路径、大小、摘要或版本无效。");
                entries.Add(id, new ProgressEntry(new SlotMetadata(id, ReadSummary(slot["summary"]), savedAt,
                    dataVersion, savedRevision, entryCommit), file, fileLength, sha));
            }
            if (!entries.TryGetValue(latest, out var latestEntry) || latestEntry.Metadata.Revision != revision
                || latestEntry.Metadata.CommitId != commit)
                throw new InvalidDataException("最近成功保存槽位与索引提交不一致。");
            return new ProgressIndex(revision, latest, commit, entries);
        }
        #endregion
    }
}
