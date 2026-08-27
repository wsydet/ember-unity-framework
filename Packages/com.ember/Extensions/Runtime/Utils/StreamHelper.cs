// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember
// Migrated from Burner extensions with cleanup.

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ember.Extensions
{
    public interface IStream
    {
        long Length { get; }
        long Position { get; set; }

        public int Read(byte[] buffer, int offset, int count);
        public int Read(IntPtr buffer, int bufferLength, int offset, int count);
        public long Seek(long offset, SeekOrigin origin);
        public void Write(byte[] buffer, int offset, int count);
        public void WriteByte(int b);
        public int ReadByte();

        public void SetLength(long value);
    }

    public class MemoryStreamProxy : IStream
    {
        public MemoryStream MStream { get; private set; }

        public long Length => MStream.Length;

        public long Position
        {
            get => MStream.Position;
            set => MStream.Position = value;
        }

        public MemoryStreamProxy(MemoryStream ms) => MStream = ms;

        public int Read(byte[] buffer, int offset, int count)
            => MStream.Read(buffer, offset, count);

        public unsafe int Read(IntPtr buffer, int bufferLength, int offset, int count)
        {
            if (!this.CheckReadWrite(bufferLength, offset, ref count)) return 0;

            var bf = MStream.GetBuffer();
            var bi = MStream.Position;

            var c = count + offset;
            var p = (byte*) buffer;
            for (int i = offset; i < c; i++)
            {
                p[i] = bf[bi++];
            }

            MStream.Position = bi;
            return count;
        }

        public int ReadByte() => MStream.ReadByte();

        public long Seek(long offset, SeekOrigin origin) => this.SeekStream(offset, origin);

        public void Write(byte[] buffer, int offset, int count) => MStream.Write(buffer, offset, count);

        public void WriteByte(int b) => MStream.WriteByte((byte)b);

        public void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }

    public unsafe class IntPtrStream : IStream
    {
        public IntPtr BytesPtr { get; private set; }
        public byte* BytesNativePtr { get; private set; }

        public long Length { get; private set; }
        private long _pos;
        public long Position
        {
            get => _pos;
            set
            {
                if (value >= 0 && value <= Length) _pos = value;
            }
        }

        public IntPtrStream(IntPtr bytes, int length) => Reset(bytes, length);

        public void Reset(IntPtr bytes, int length)
        {
            if (bytes == IntPtr.Zero)
            {
                throw new ArgumentException("[Ember]: cannot set null IntPtr");
            }

            BytesPtr = bytes;
            BytesNativePtr = (byte*) bytes;
            Length = length;
            Position = 0;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!this.CheckReadWrite(buffer.Length, offset, ref count)) return 0;

            for (int i = 0, j = (int)Position; i < count; i++, j++)
            {
                buffer[offset++] = BytesNativePtr[j];
            }

            Position += count;
            return count;
        }

        public int Read(IntPtr buffer, int bufferLength, int offset, int count)
        {
            if (!this.CheckReadWrite(bufferLength, offset, ref count)) return 0;

            var p = (byte*) buffer;
            for (int i = 0, j = (int)Position; i < count; i++, j++)
            {
                p[offset++] = BytesNativePtr[j];
            }

            Position += count;
            return count;
        }


        public long Seek(long offset, SeekOrigin origin) => this.SeekStream(offset, origin);

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!this.CheckReadWrite(buffer.Length, offset, ref count)) return;

            for (int i = 0, j = (int)Position; i < count; i++, j++)
            {
                BytesNativePtr[j] = buffer[offset++];
            }

            Position += count;
        }

        public void WriteByte(int b)
        {
            BytesNativePtr[Position++] = (byte)b;
        }

        public int ReadByte()
        {
            if (Position >= Length)
            {
                throw new Exception($"[Ember]: position {Position} out of stream length {Length}");
            }

            if (BytesNativePtr == null)
            {
                throw new Exception("[Ember]: IntPtrStream BytesNativePtr == null");
            }

            try
            {
                return BytesNativePtr[Position++];
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }

    public class BytesStream : IStream
    {
        public byte[] Bytes { get; private set; }

        public long Length { get; private set; }
        private long _pos;
        public long Position
        {
            get => _pos;
            set
            {
                if (value >= 0 && value <= Length) _pos = value;
            }
        }

        public BytesStream(byte[] bytes) => Reset(bytes);
        public void Reset(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException(
                    "[Ember]: the value of bytes array length must be greater than zero");
            }

            Bytes = bytes;
            Length = bytes.Length;
            Position = 0;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!this.CheckReadWrite(buffer.Length, offset, ref count)) return 0;

            for (int i = 0, j = (int)Position; i < count; i++, j++)
            {
                buffer[offset++] = Bytes[j];
            }

            Position += count;
            return count;
        }

        public unsafe int Read(IntPtr buffer, int bufferLength, int offset, int count)
        {
            if (!this.CheckReadWrite(bufferLength, offset, ref count)) return 0;

            var p = (byte*) buffer;
            for (int i = 0, j = (int)Position; i < count; i++, j++)
            {
                p[offset++] = Bytes[j];
            }

            Position += count;
            return count;
        }

        public long Seek(long offset, SeekOrigin origin) => this.SeekStream(offset, origin);

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!this.CheckReadWrite(buffer.Length, offset, ref count)) return;

            for (int i = 0, j = (int)Position; i < count; i++, j++)
            {
                Bytes[j] = buffer[offset++];
            }

            Position += count;
        }

        public void WriteByte(int b)
        {
            Bytes[Position++] = (byte)b;
        }

        public int ReadByte()
        {
            return Bytes[Position++];
        }

        public void SetLength(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentException("[Ember]: the value of setting length must be greater than zero");
            }

            if (value > int.MaxValue)
            {
                throw new ArgumentException($"[Ember]: the value of setting length must be less than int.MaxValue {int.MaxValue}");
            }

            var originLength = Bytes.Length;
            var newBytes = new byte[value];

            var count = Math.Min((int)value, originLength);
            for (int i = 0; i < count; i++)
            {
                newBytes[i] = Bytes[i];
            }

            Bytes = newBytes;
        }
    }

    public static class StreamHelper
    {
        public static void WriteLong7Bit(this IStream stream, long val)
        {
            if (val < 0)
            {
                throw new Exception("[Ember]: it cannot write negative value!");
            }

            if (val == 0)
            {
                stream.WriteByte(0);
                return;
            }

            while (val > 0)
            {
                var b = (byte)(val & 0x7F);
                val >>= 7;
                if (val > 0) b |= 0x80;
                stream.WriteByte(b);
            }
        }

        public static long ReadLong7Bit(this IStream stream)
        {
            long result = 0;
            long b = stream.ReadByte();

            if (b == 0) return result;

            var shift = 0;
            do
            {
                result |= (b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                {
                    break;
                }

                b = stream.ReadByte();
                shift += 7;
            }
            while (true);

            return result;
        }

        public static void WriteInt7Bit(this IStream stream, int v) => WriteLong7Bit(stream, v);
        public static int ReadInt7Bit(this IStream stream) => checked((int) ReadLong7Bit(stream));

        public static void WriteString7Bit(this IStream stream, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                WriteInt7Bit(stream, 0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(str);
            WriteInt7Bit(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static unsafe string ReadString7Bit(this IStream stream)
        {
            var len = ReadInt7Bit(stream);
            if (len <= 0) return string.Empty;

            switch (stream)
            {
                case BytesStream bytesStream:
                {
                    var str = Encoding.UTF8.GetString(bytesStream.Bytes, (int)bytesStream.Position, len);
                    bytesStream.Position += len;
                    return str;
                }
                case IntPtrStream bytesNativeStream:
                {
                    var str = Encoding.UTF8.GetString(bytesNativeStream.BytesNativePtr, len);
                    bytesNativeStream.Position += len;
                    return str;
                }
            }

            var bytes = new byte[len];
            stream.Read(bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        public static void WriteInts7Bit(this IStream stream, int[] arr)
        {
            stream.WriteInt7Bit(arr.Length);
            foreach (var i in arr)
            {
                stream.WriteInt7Bit(i);
            }
        }

        public static int[] ReadInts7Bit(this IStream stream, ref int[] ints, out int count)
        {
            count = stream.ReadInt7Bit();
            if (count > 0)
            {
                if (ints == null || ints.Length < count)
                {
                    ints = new int[count];
                }

                for (int i = 0; i < count; i++)
                {
                    ints[i] = stream.ReadInt7Bit();
                }

                return ints;
            }

            return null;
        }

        public static int[] ReadInts7Bit(this IStream stream)
        {
            int[] ints = null;
            int count;
            return ReadInts7Bit(stream, ref ints, out count);
        }

        public static void WriteInt64(this IStream stream, long val)
        {
            stream.WriteByte((byte)(val & 0xff));
            stream.WriteByte((byte)((val >> 8) & 0xff));
            stream.WriteByte((byte)((val >> 16) & 0xff));
            stream.WriteByte((byte)((val >> 24) & 0xff));

            stream.WriteByte((byte)((val >> 32) & 0xff));
            stream.WriteByte((byte)((val >> 40) & 0xff));
            stream.WriteByte((byte)((val >> 48) & 0xff));
            stream.WriteByte((byte)((val >> 56)));
        }

        public static long ReadInt64(this IStream stream)
        {
            uint low = (uint)stream.ReadByte()
                         | ((uint)stream.ReadByte() << 8)
                         | ((uint)stream.ReadByte() << 16)
                         | ((uint)stream.ReadByte() << 24);

            uint high = (uint)stream.ReadByte()
                          | ((uint)stream.ReadByte() << 8)
                          | ((uint)stream.ReadByte() << 16)
                          | ((uint)stream.ReadByte() << 24);

            return (long) (((long) high) << 32) | (long) low;
        }

        public static void WriteInt32(this IStream stream, int val)
        {
            stream.WriteByte((byte)(val & 0xff));
            stream.WriteByte((byte)((val >> 8) & 0xff));
            stream.WriteByte((byte)((val >> 16) & 0xff));
            stream.WriteByte((byte)((val >> 24)));
        }

        public static int ReadInt32(this IStream stream)
        {
            return stream.ReadByte()
                   | (stream.ReadByte() << 8)
                   | (stream.ReadByte() << 16)
                   | (stream.ReadByte() << 24);
        }

        public static void WriteInt16(this IStream stream, int v) => WriteInt16(stream, checked((short) v));
        public static void WriteInt16(this IStream stream, short v)
        {
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
        }

        public static short ReadInt16(this IStream stream)
        {
            return (short)(stream.ReadByte() | (stream.ReadByte() << 8));
        }

        public static void WriteInt8(this IStream stream, sbyte v) => stream.WriteByte(v);
        public static sbyte ReadInt8(this IStream stream) => (sbyte)stream.ReadByte();

        public static void WriteUInt32(this IStream stream, uint val)
        {
            stream.WriteByte((byte)(val & 0xff));
            stream.WriteByte((byte)((val >> 8) & 0xff));
            stream.WriteByte((byte)((val >> 16) & 0xff));
            stream.WriteByte((byte)((val >> 24)));
        }

        public static uint ReadUInt32(this IStream stream)
        {
            return (uint)stream.ReadByte()
                   | ((uint)stream.ReadByte() << 8)
                   | ((uint)stream.ReadByte() << 16)
                   | ((uint)stream.ReadByte() << 24);
        }

        public static void WriteUInt16(this IStream stream, uint v) => WriteUInt16(stream, checked((ushort) v));
        public static void WriteUInt16(this IStream stream, ushort v)
        {
            stream.WriteByte((byte)(v & 0xff));
            stream.WriteByte((byte)((v >> 8) & 0xff));
        }

        public static ushort ReadUInt16(this IStream stream) =>
            (ushort) ((ushort) stream.ReadByte() | ((ushort) stream.ReadByte() << 8));

        public static byte ReadUInt8(this IStream stream) => (byte)stream.ReadByte();
        public static void WriteUInt8(this IStream stream, byte v) => stream.WriteByte(v);

        public static unsafe void WriteFloat(this IStream stream, float v)
        {
            uint i = *(uint*)&v;
            stream.WriteUInt32(i);
        }

        public static unsafe float ReadFloat(this IStream stream)
        {
            var i = stream.ReadUInt32();
            return *(float*) &i;
        }

        public static bool CheckReadWrite(this IStream stream, int bufferLength, int offset, ref int count)
        {
            if (offset < 0 || offset >= bufferLength || count <= 0)
            {
                return false;
            }

            if (offset + count > bufferLength)
            {
                count = bufferLength - offset;
            }

            if (stream.Length - stream.Position < count)
            {
                count = (int)(stream.Length - stream.Position);
            }

            return true;
        }

        public static long SeekStream(this IStream stream, long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    stream.Position = Math.Min((int)stream.Length, checked((int)offset));
                    break;
                case SeekOrigin.Current:
                    stream.Position = Math.Min((int)stream.Length, checked((int)(stream.Position + offset)));
                    break;
                case SeekOrigin.End:
                    stream.Position = Math.Min((int)stream.Length, checked((int)(stream.Length + offset)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return stream.Position;
        }
    }
}
