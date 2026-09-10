// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Ember.Basic;

namespace Ember.Table
{
    /// <summary>固定小端、严格边界和严格 UTF-8 的表二进制读取器。</summary>
    public sealed class EmberTableBinaryReader
    {
        #region 内部参数

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private readonly byte[] _buffer;
        private readonly int _end;
        private readonly int _maxStringBytes;
        private int _position;

        public int Position => _position;
        public int Remaining => _end - _position;
        public bool IsFullyConsumed => _position == _end;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public EmberTableBinaryReader(
            byte[] buffer,
            int offset = 0,
            int length = -1,
            int maxStringBytes = EmberTableCatalogEntry.DEFAULT_MAX_STRING_BYTES)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) length = buffer.Length - offset;
            if (length < 0 || length > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (maxStringBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(maxStringBytes));

            _position = offset;
            _end = offset + length;
            _maxStringBytes = maxStringBytes;
        }

        [NoGC]
        public byte ReadByte()
        {
            EnsureAvailable(1);
            return _buffer[_position++];
        }

        [HasGC]
        public bool ReadBoolean()
        {
            byte value = ReadByte();
            if (value > 1)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidBoolean,
                    $"Boolean marker must be 0 or 1, got {value}.");
            return value == 1;
        }

        [HasGC]
        public bool ReadNullableMarker()
        {
            byte value = ReadByte();
            if (value > 1)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidNullableMarker,
                    $"Nullable marker must be 0 or 1, got {value}.");
            return value == 1;
        }

        [NoGC]
        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadByte());
        }

        [NoGC]
        public short ReadInt16()
        {
            return unchecked((short)ReadUInt16());
        }

        [NoGC]
        public ushort ReadUInt16()
        {
            EnsureAvailable(2);
            int p = _position;
            _position += 2;
            return (ushort)(_buffer[p] | (_buffer[p + 1] << 8));
        }

        [NoGC]
        public int ReadInt32()
        {
            return unchecked((int)ReadUInt32());
        }

        [NoGC]
        public uint ReadUInt32()
        {
            EnsureAvailable(4);
            int p = _position;
            _position += 4;
            return (uint)(_buffer[p]
                          | (_buffer[p + 1] << 8)
                          | (_buffer[p + 2] << 16)
                          | (_buffer[p + 3] << 24));
        }

        [NoGC]
        public long ReadInt64()
        {
            return unchecked((long)ReadUInt64());
        }

        [NoGC]
        public ulong ReadUInt64()
        {
            uint low = ReadUInt32();
            uint high = ReadUInt32();
            return low | ((ulong)high << 32);
        }

        [NoGC]
        public float ReadSingle()
        {
            return BitConverter.Int32BitsToSingle(ReadInt32());
        }

        [NoGC]
        public double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble(ReadInt64());
        }

        [HasGC]
        public decimal ReadDecimal()
        {
            return new decimal(new[] { ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32() });
        }

        [HasGC]
        public string ReadString()
        {
            int byteLength = ReadInt32();
            if (byteLength == -1) return null;
            if (byteLength < -1)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidLength,
                    $"String byte length cannot be {byteLength}.");
            return ReadUtf8(byteLength, _maxStringBytes);
        }

        [HasGC]
        public string ReadUtf8(int byteLength, int maxByteLength)
        {
            if (byteLength < 0)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidLength,
                    $"UTF-8 byte length cannot be {byteLength}.");
            if (byteLength > maxByteLength)
                throw new EmberTableDataException(
                    EmberTableErrorCode.LimitExceeded,
                    $"UTF-8 byte length {byteLength} exceeds limit {maxByteLength}.");
            EnsureAvailable(byteLength);
            try
            {
                string value = StrictUtf8.GetString(_buffer, _position, byteLength);
                _position += byteLength;
                return value;
            }
            catch (DecoderFallbackException ex)
            {
                throw new EmberTableDataException(EmberTableErrorCode.InvalidUtf8, ex.Message);
            }
        }

        [HasGC]
        public byte[] ReadBytes(int length)
        {
            if (length < 0)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidLength,
                    $"Byte length cannot be {length}.");
            EnsureAvailable(length);
            var result = new byte[length];
            Buffer.BlockCopy(_buffer, _position, result, 0, length);
            _position += length;
            return result;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void EnsureAvailable(int count)
        {
            if (count < 0 || count > Remaining)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidLength,
                    $"Binary payload is truncated at byte {_position}; requested {count}, remaining {Remaining}.");
        }

        #endregion
    }

    /// <summary>固定小端和严格 UTF-8 的确定性表二进制写入器。</summary>
    public sealed class EmberTableBinaryWriter
    {
        #region 内部参数

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly List<byte> _buffer;

        public int Length => _buffer.Count;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public EmberTableBinaryWriter(int capacity = 256)
        {
            _buffer = new List<byte>(Math.Max(0, capacity));
        }

        [HasGC]
        public void WriteByte(byte value)
        {
            _buffer.Add(value);
        }

        [HasGC]
        public void WriteBoolean(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        [HasGC]
        public void WriteNullableMarker(bool hasValue)
        {
            WriteBoolean(hasValue);
        }

        [HasGC]
        public void WriteSByte(sbyte value)
        {
            WriteByte(unchecked((byte)value));
        }

        [HasGC]
        public void WriteInt16(short value)
        {
            WriteUInt16(unchecked((ushort)value));
        }

        [HasGC]
        public void WriteUInt16(ushort value)
        {
            _buffer.Add((byte)value);
            _buffer.Add((byte)(value >> 8));
        }

        [HasGC]
        public void WriteInt32(int value)
        {
            WriteUInt32(unchecked((uint)value));
        }

        [HasGC]
        public void WriteUInt32(uint value)
        {
            _buffer.Add((byte)value);
            _buffer.Add((byte)(value >> 8));
            _buffer.Add((byte)(value >> 16));
            _buffer.Add((byte)(value >> 24));
        }

        [HasGC]
        public void WriteInt64(long value)
        {
            WriteUInt64(unchecked((ulong)value));
        }

        [HasGC]
        public void WriteUInt64(ulong value)
        {
            WriteUInt32((uint)value);
            WriteUInt32((uint)(value >> 32));
        }

        [HasGC]
        public void WriteSingle(float value)
        {
            WriteInt32(BitConverter.SingleToInt32Bits(value));
        }

        [HasGC]
        public void WriteDouble(double value)
        {
            WriteInt64(BitConverter.DoubleToInt64Bits(value));
        }

        [HasGC]
        public void WriteDecimal(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            for (int i = 0; i < bits.Length; i++) WriteInt32(bits[i]);
        }

        [HasGC]
        public void WriteString(string value, int maxByteLength = EmberTableCatalogEntry.DEFAULT_MAX_STRING_BYTES)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            byte[] bytes = StrictUtf8.GetBytes(value);
            if (bytes.Length > maxByteLength)
                throw new EmberTableDataException(
                    EmberTableErrorCode.LimitExceeded,
                    $"UTF-8 byte length {bytes.Length} exceeds limit {maxByteLength}.");
            WriteInt32(bytes.Length);
            WriteBytes(bytes);
        }

        [HasGC]
        public void WriteHeaderString(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Header identifier cannot be null or empty.", nameof(value));
            byte[] bytes = StrictUtf8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Header identifier is too long.");
            WriteUInt16((ushort)bytes.Length);
            WriteBytes(bytes);
        }

        [HasGC]
        public void WriteBytes(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            _buffer.AddRange(bytes);
        }

        [HasGC]
        public byte[] ToArray()
        {
            return _buffer.ToArray();
        }

        #endregion
    }

    /// <summary>V1 文件头的已验证只读视图。</summary>
    public sealed class EmberTableBinaryHeader
    {
        public ushort FormatVersion { get; }
        public ushort Flags { get; }
        public string TableId { get; }
        public string RowTypeId { get; }
        public string SchemaHash { get; }
        public string SourceHash { get; }
        public int RowCount { get; }
        public int PayloadLength { get; }
        public string PayloadHash { get; }

        public EmberTableBinaryHeader(
            ushort formatVersion,
            ushort flags,
            string tableId,
            string rowTypeId,
            string schemaHash,
            string sourceHash,
            int rowCount,
            int payloadLength,
            string payloadHash)
        {
            FormatVersion = formatVersion;
            Flags = flags;
            TableId = tableId;
            RowTypeId = rowTypeId;
            SchemaHash = schemaHash;
            SourceHash = sourceHash;
            RowCount = rowCount;
            PayloadLength = payloadLength;
            PayloadHash = payloadHash;
        }
    }

    /// <summary>冻结的 ETBL V1 文件构建和 SHA-256 表示。</summary>
    public static class EmberTableBinaryFormat
    {
        public const ushort VERSION = 1;
        public const ushort FLAGS = 0;
        public const int HASH_BYTES = 32;
        public const string MAGIC = "ETBL";

        [HasGC]
        public static byte[] BuildFile(
            string tableId,
            string rowTypeId,
            byte[] schemaHash,
            byte[] sourceHash,
            int rowCount,
            byte[] payload)
        {
            ValidateHash(schemaHash, nameof(schemaHash));
            ValidateHash(sourceHash, nameof(sourceHash));
            if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            byte[] payloadHash = ComputeSha256(payload);
            var writer = new EmberTableBinaryWriter(128 + payload.Length);
            writer.WriteBytes(Encoding.ASCII.GetBytes(MAGIC));
            writer.WriteUInt16(VERSION);
            writer.WriteUInt16(FLAGS);
            writer.WriteHeaderString(tableId);
            writer.WriteHeaderString(rowTypeId);
            writer.WriteBytes(schemaHash);
            writer.WriteBytes(sourceHash);
            writer.WriteInt32(rowCount);
            writer.WriteInt32(payload.Length);
            writer.WriteBytes(payloadHash);
            writer.WriteBytes(payload);
            return writer.ToArray();
        }

        [HasGC]
        public static byte[] ComputeSha256(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            using (SHA256 sha = SHA256.Create()) return sha.ComputeHash(bytes);
        }

        [HasGC]
        public static byte[] ComputeSha256(byte[] bytes, int offset, int count)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (offset < 0 || count < 0 || count > bytes.Length - offset)
                throw new ArgumentOutOfRangeException();
            using (SHA256 sha = SHA256.Create()) return sha.ComputeHash(bytes, offset, count);
        }

        [HasGC]
        public static string ToHex(byte[] bytes)
        {
            if (bytes == null) return null;
            var chars = new char[bytes.Length * 2];
            const string alphabet = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = alphabet[bytes[i] >> 4];
                chars[i * 2 + 1] = alphabet[bytes[i] & 15];
            }
            return new string(chars);
        }

        [HasGC]
        public static byte[] ParseHash(string hex)
        {
            if (hex == null || hex.Length != HASH_BYTES * 2)
                throw new FormatException("SHA-256 must contain exactly 64 hexadecimal characters.");
            var bytes = new byte[HASH_BYTES];
            for (int i = 0; i < bytes.Length; i++)
            {
                int high = ParseHex(hex[i * 2]);
                int low = ParseHex(hex[i * 2 + 1]);
                if (high < 0 || low < 0) throw new FormatException("SHA-256 contains a non-hex character.");
                bytes[i] = (byte)((high << 4) | low);
            }
            return bytes;
        }

        [NoGC]
        public static bool HashEquals(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static void ValidateHash(byte[] hash, string parameterName)
        {
            if (hash == null || hash.Length != HASH_BYTES)
                throw new ArgumentException("SHA-256 value must contain exactly 32 bytes.", parameterName);
        }

        private static int ParseHex(char value)
        {
            if (value >= '0' && value <= '9') return value - '0';
            if (value >= 'a' && value <= 'f') return value - 'a' + 10;
            if (value >= 'A' && value <= 'F') return value - 'A' + 10;
            return -1;
        }
    }
}
