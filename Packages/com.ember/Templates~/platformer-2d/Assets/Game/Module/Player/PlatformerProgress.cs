using System;
using System.IO;
using System.Text;
using Game.PlayerData;
using UnityEngine;

namespace Game.Player
{
    /// <summary>历史 v1 数据，仅保留用于旧版本拒绝及通用存储回归夹具。正式玩家只使用 PlatformerSnapshot v2。</summary>
    public sealed class PlatformerProgress
    {
        public int Version = 1;
        public string Scene;
        public float X, Y, VelocityX, VelocityY;
        public bool FacesRight = true;
        public Vector2 Position => new(X, Y);
        public Vector2 Velocity => new(VelocityX, VelocityY);
        public void Validate()
        {
            if (Version != 1 || string.IsNullOrWhiteSpace(Scene) || Scene.Length > 128 ||
                !float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(VelocityX) || !float.IsFinite(VelocityY))
                throw new ArgumentException("平台进度版本、场景或运动数据无效。");
        }
    }

    public sealed class PlatformerProgressCodec : IProgressCodec<PlatformerProgress>
    {
        public string DataTypeId => "ember.platformer2d.progress";
        public EncodedProgress Encode(PlatformerProgress snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            snapshot.Validate();
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true))
            {
                writer.Write(snapshot.Version); writer.Write(snapshot.Scene);
                writer.Write(snapshot.X); writer.Write(snapshot.Y);
                writer.Write(snapshot.VelocityX); writer.Write(snapshot.VelocityY); writer.Write(snapshot.FacesRight);
            }
            return new EncodedProgress(stream.ToArray(), snapshot.Version);
        }

        public DecodeResult<PlatformerProgress> Decode(ReadOnlyMemory<byte> payload, int dataVersion)
        {
            if (dataVersion != 1) return new DecodeResult<PlatformerProgress>(DecodeStatus.DataVersionUnsupported);
            try
            {
                using var stream = new MemoryStream(payload.ToArray(), false);
                using var reader = new BinaryReader(stream, new UTF8Encoding(false, true));
                var data = new PlatformerProgress { Version = reader.ReadInt32(), Scene = reader.ReadString(),
                    X = reader.ReadSingle(), Y = reader.ReadSingle(), VelocityX = reader.ReadSingle(), VelocityY = reader.ReadSingle() };
                byte facing = reader.ReadByte();
                if (facing > 1 || stream.Position != stream.Length || data.Version != dataVersion)
                    throw new InvalidDataException("平台进度字段或版本不一致。");
                data.FacesRight = facing == 1;
                data.Validate();
                return new DecodeResult<PlatformerProgress>(DecodeStatus.Decoded, data);
            }
            catch (Exception error) { return new DecodeResult<PlatformerProgress>(DecodeStatus.DecodeFailed, error: error.Message); }
        }
    }
}
