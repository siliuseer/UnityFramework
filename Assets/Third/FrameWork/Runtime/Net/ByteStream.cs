using System;
using System.IO;

namespace siliu
{
    public class ByteStream : MemoryStream
    {
        public sbyte ReadSbyte()
        {
            return (sbyte)ReadByte();
        }

        public void WriteSByte(sbyte value)
        {
            WriteByte(Convert.ToByte(value));
        }

        public int ReadShort()
        {
            return ((ReadByte() & 0xff) << 8) | (ReadByte() & 0xff);
        }

        public void WritShort(int value)
        {
            WriteByte((byte)((value >> 8) & 0xff));
            WriteByte((byte)(value & 0xff));
        }
        public ushort ReadUShort()
        {
            return (ushort)(((ReadByte() & 0xff) << 8) | (ReadByte() & 0xff));
        }

        public void WritUShort(int value)
        {
            if (value > ushort.MaxValue)
            {
                throw new Exception(value + " > " + ushort.MaxValue);
            }
            WriteByte((byte)((value >> 8) & 0xff));
            WriteByte((byte)(value & 0xff));
        }

        public int ReadInt32()
        {
            return ((ReadByte() & 0xff) << 24) | ((ReadByte() & 0xff) << 16) | ((ReadByte() & 0xff) << 8) | (ReadByte() & 0xff);
        }

        public void WritInt(int value)
        {
            WriteByte((byte)((value >> 24) & 0xff));
            WriteByte((byte)((value >> 16) & 0xff));
            WriteByte((byte)((value >> 8) & 0xff));
            WriteByte((byte)(value & 0xff));
        }

        public byte[] ReadBytes(long count)
        {
            long len = Available;
            if (len < count)
            {
                // Debug.LogError("剩余长度["+len+"]不够, 直接读取剩余全部:"+count);
                count = len;
            }

            byte[] bytes = new byte[count];
            Read(bytes, 0, (int)count);

            return bytes;
        }

        public void SeekToEnd()
        {
            if (!CanSeek)
                return;

            Seek(0, SeekOrigin.End);
        }

        public void SeekToBegin()
        {
            if (!CanSeek)
                return;

            Seek(0, SeekOrigin.Begin);
        }

        public void SeekOffset(int offset)
        {
            if (!CanSeek)
                return;

            Seek(offset, SeekOrigin.Current);
        }

        public void Append(byte[] buffer, int offset, int count)
        {
            SeekToEnd();
            Write(buffer, offset, count);
        }

        public void Clear()
        {
            Flush();
            SetLength(0);
        }

        public void ConvertToAvailable()
        {
            var buffer = new byte[Available];
            var count = Read(buffer, 0, buffer.Length);
            SetLength(0);
            Write(buffer, 0, count);
            Seek(0, SeekOrigin.Begin);
        }

        public string AvailableByteString
        {
            get
            {
                var buffer = new byte[Available];
                Array.Copy(GetBuffer(), Position, buffer, 0, buffer.Length);
                return string.Join(",", buffer);
            }
        }

        public long Available => Length - Position;
    }
}
