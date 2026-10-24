using System;
using System.IO;
using System.Text;
using Game.Entity;
using Game.PlayerData;
using UnityEngine;

namespace Game.Player
{
    /// <summary>项目业务 v3 格式。旧版本默认明确拒绝，业务可显式注入迁移器。</summary>
    public sealed class PlatformerSnapshotCodec : IProgressCodec<PlatformerSnapshot>
    {
        #region 内部参数
        private const int MAX_BYTES = 1024 * 1024;
        public string DataTypeId => "ember.platformer2d.progress";
        private readonly IPlatformerSnapshotMigration _migration;
        public PlatformerSnapshotCodec(IPlatformerSnapshotMigration migration = null) => _migration = migration;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void WritePowers(BinaryWriter writer, EntityPowerSnapshot powers, bool skill)
        {
            int count = skill ? powers.SkillCount : powers.AbilityCount;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                var item = skill ? powers.GetSkill(i) : powers.GetAbility(i);
                writer.Write(item.Id); writer.Write(item.IsOpen); writer.Write(item.RemainingUses ?? -1); writer.Write(item.RemainingCooldown);
            }
        }
        private EntityPowerRecord[] ReadPowers(BinaryReader reader)
        {
            var result = new EntityPowerRecord[ReadCount(reader, 256)];
            for (int i = 0; i < result.Length; i++)
            {
                string id = ReadText(reader, 256); bool open = ReadBool(reader); int uses = reader.ReadInt32();
                if (uses < -1) throw new InvalidDataException("次数标记无效。");
                result[i] = new EntityPowerRecord(id, open, uses == -1 ? null : uses, reader.ReadSingle());
            }
            return result;
        }
        private int ReadCount(BinaryReader reader, int maximum)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum) throw new InvalidDataException("快照集合长度越界。");
            return count;
        }
        private bool ReadBool(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("快照布尔标记无效。");
            return value != 0;
        }
        private string ReadText(BinaryReader reader, int maximum)
        {
            string value = reader.ReadString();
            if (value.Length > maximum) throw new InvalidDataException("快照字符串过长。");
            return value;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EncodedProgress Encode(PlatformerSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true))
            {
                writer.Write(PlatformerSnapshot.DATA_VERSION); writer.Write(snapshot.Scene);
                writer.Write(snapshot.Position.x); writer.Write(snapshot.Position.y);
                writer.Write(snapshot.Velocity.x); writer.Write(snapshot.Velocity.y); writer.Write(snapshot.FacesRight);
                writer.Write(snapshot.Life.Installed); writer.Write(snapshot.Life.Health);
                WritePowers(writer, snapshot.Powers, false); WritePowers(writer, snapshot.Powers, true);
                writer.Write(snapshot.EffectsInstalled); writer.Write(snapshot.EffectCount);
                for (int i = 0; i < snapshot.EffectCount; i++)
                {
                    var effect = snapshot.GetEffect(i);
                    writer.Write(effect.EffectId); writer.Write(effect.SourceKey); writer.Write((byte)effect.Kind);
                    writer.Write((byte)EntityEffectLifetime.Timed); writer.Write(effect.RemainingSeconds);
                    writer.Write(effect.Rules.DamageCategoryCount);
                    for (int n = 0; n < effect.Rules.DamageCategoryCount; n++) writer.Write(effect.Rules.GetDamageCategory(n));
                    writer.Write(effect.Rules.PreventsStagger); writer.Write(effect.Rules.ExcludedLayers);
                    writer.Write(effect.Appearance.Enabled); writer.Write(effect.Appearance.Priority);
                    writer.Write(effect.Appearance.Alpha); writer.Write(effect.Appearance.BlinkHz);
                }
            }
            if (stream.Length > MAX_BYTES) throw new InvalidDataException("完整快照超过 1 MiB。");
            return new EncodedProgress(stream.ToArray(), PlatformerSnapshot.DATA_VERSION);
        }
        public DecodeResult<PlatformerSnapshot> Decode(ReadOnlyMemory<byte> payload, int dataVersion)
        {
            if (_migration != null) return _migration.Migrate(payload, dataVersion);
            if (dataVersion != PlatformerSnapshot.DATA_VERSION)
                return new DecodeResult<PlatformerSnapshot>(DecodeStatus.DataVersionUnsupported,
                    error: "只支持完整进度 v3；旧档请重新开始或显式迁移版本及能力装配集合，原文件保留，不自动补满血。");
            try
            {
                if (payload.Length > MAX_BYTES) throw new InvalidDataException("完整快照超过 1 MiB。");
                using var stream = new MemoryStream(payload.ToArray(), false);
                using var reader = new BinaryReader(stream, new UTF8Encoding(false, true));
                if (reader.ReadInt32() != dataVersion) throw new InvalidDataException("快照内外版本不一致。");
                string scene = ReadText(reader, 128);
                var position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                var velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle()); bool facing = ReadBool(reader);
                var life = new PlatformerLifeRecord(ReadBool(reader), reader.ReadSingle());
                var powers = new EntityPowerSnapshot(ReadPowers(reader), ReadPowers(reader));
                bool installed = ReadBool(reader);
                var effects = new EntityTimedEffectSnapshot[ReadCount(reader, 4096)];
                for (int i = 0; i < effects.Length; i++)
                {
                    string id = ReadText(reader, 256), source = ReadText(reader, 256);
                    var kind = (EntityEffectKind)reader.ReadByte();
                    if (reader.ReadByte() != (byte)EntityEffectLifetime.Timed) throw new InvalidDataException("效果 " + id + " 缺少来源恢复适配。");
                    float remaining = reader.ReadSingle();
                    var categories = new string[ReadCount(reader, 64)];
                    for (int n = 0; n < categories.Length; n++) categories[n] = ReadText(reader, 256);
                    var rules = new EntityEffectRules(categories, ReadBool(reader), reader.ReadInt32());
                    var appearance = new EntityEffectAppearance(ReadBool(reader), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle());
                    effects[i] = new EntityTimedEffectSnapshot(id, source, remaining, rules, appearance, kind);
                }
                if (stream.Position != stream.Length) throw new InvalidDataException("完整快照包含多余数据。");
                return new DecodeResult<PlatformerSnapshot>(DecodeStatus.Decoded,
                    new PlatformerSnapshot(scene, position, velocity, facing, powers, installed, effects, life));
            }
            catch (Exception error) { return new DecodeResult<PlatformerSnapshot>(DecodeStatus.DecodeFailed, error: error.Message); }
        }
        #endregion
    }
    /// <summary>迁移需由业务显式处理版本与目标能力装配集合；返回值仍经过完整候选准备校验。</summary>
    public interface IPlatformerSnapshotMigration
    { DecodeResult<PlatformerSnapshot> Migrate(ReadOnlyMemory<byte> payload, int dataVersion); }
}
