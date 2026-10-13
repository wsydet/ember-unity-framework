using System;

namespace Game.PlayerData
{
    /// <summary>只允许存储程序集访问冻结字节；不保留调用方快照或 codec 输出数组。</summary>
    internal sealed class FrozenProgress
    {
        internal string DataTypeId { get; }
        internal int DataVersion { get; }
        internal byte[] Payload { get; }
        internal SaveSummary Summary { get; }

        internal FrozenProgress(string dataTypeId, int dataVersion, byte[] payload, SaveSummary summary)
        { DataTypeId = dataTypeId; DataVersion = dataVersion; Payload = payload; Summary = summary; }
    }

    internal static class ProgressSnapshot
    {
        #region 外部方法
        /// <summary>由请求入口在调用线程同步执行；成功返回 null，失败返回本请求终态。</summary>
        internal static SaveResult TryFreeze<T>(ProgressRequestContext context, ProgressStoreOptions options,
            T snapshot, SaveSummary summary, IProgressCodec<T> codec, out FrozenProgress frozen)
        {
            frozen = null;
            if (!context.SlotId.HasValue || !options.ContainsSlot(context.SlotId.Value))
                return new SaveResult(context, SaveStatus.InvalidSlot);
            try
            {
                if (codec == null || !string.Equals(codec.DataTypeId, options.DataTypeId, StringComparison.Ordinal))
                    return new SaveResult(context, SaveStatus.WrongDataType);
                if (snapshot == null || summary == null) return new SaveResult(context, SaveStatus.EncodeFailed, "快照与摘要必须显式提供。");
                var encoded = codec.Encode(snapshot);
                if (encoded?.Payload == null || encoded.DataVersion <= 0)
                    return new SaveResult(context, SaveStatus.EncodeFailed, "codec 未提供有效负载或业务版本。");
                if (encoded.Payload.Length > options.MaxPayloadBytes)
                    return new SaveResult(context, SaveStatus.TooLarge);
                frozen = new FrozenProgress(options.DataTypeId, encoded.DataVersion, (byte[])encoded.Payload.Clone(),
                    new SaveSummary(summary.Title, summary.Description, summary.Metadata));
                return null;
            }
            catch (Exception exception) { return new SaveResult(context, SaveStatus.EncodeFailed, exception.ToString()); }
        }

        /// <summary>存储读取完成后回到调用线程解码；成功仅表示数据解码，不表示游戏恢复。</summary>
        internal static ReadResult<T> Decode<T>(StoredPayload stored, string dataTypeId, IProgressCodec<T> codec)
        {
            if (stored.Status != ReadStatus.Loaded)
                return new ReadResult<T>(stored.Context, stored.Status, stored.Error);
            try
            {
                if (codec == null || !string.Equals(codec.DataTypeId, dataTypeId, StringComparison.Ordinal))
                    return new ReadResult<T>(stored.Context, ReadStatus.WrongDataType);
                var decoded = codec.Decode(new ReadOnlyMemory<byte>(stored.Payload), stored.Slot.DataVersion.Value);
                if (decoded == null) return new ReadResult<T>(stored.Context, ReadStatus.DecodeFailed, "codec 未返回结果。");
                if (decoded.Status == DecodeStatus.DataVersionUnsupported)
                    return new ReadResult<T>(stored.Context, ReadStatus.DataVersionUnsupported, decoded.Error);
                if (decoded.Status != DecodeStatus.Decoded || decoded.Progress == null)
                    return new ReadResult<T>(stored.Context, ReadStatus.DecodeFailed, decoded.Error);
                return new ReadResult<T>(stored.Context, ReadStatus.Loaded, progress: decoded.Progress, slot: stored.Slot);
            }
            catch (Exception exception) { return new ReadResult<T>(stored.Context, ReadStatus.DecodeFailed, exception.ToString()); }
        }
        #endregion
    }

    internal sealed class StoredPayload
    {
        internal ProgressRequestContext Context { get; }
        internal ReadStatus Status { get; }
        internal byte[] Payload { get; }
        internal SlotMetadata Slot { get; }
        internal string Error { get; }

        internal StoredPayload(ProgressRequestContext context, ReadStatus status, byte[] payload = null,
            SlotMetadata slot = null, string error = null)
        { Context = context; Status = status; Payload = payload; Slot = slot; Error = error; }
    }
}
