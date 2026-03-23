using System.Buffers.Binary;

namespace ProxyServer.Buffer
{
    public class WriteBuffer
    {
        private readonly MemoryStream _ms;
        private int _offset;

        public EndianType EndianType { get; set; } = EndianType.Little;
        public int Length => _offset;

        public WriteBuffer()
        {
            _ms = new MemoryStream();
            _offset = 0;
        }

        public byte[] ToArray() => _ms.ToArray();

        private void Write(byte value)
        {
            _ms.WriteByte(value);
            _offset++;
        }

        public void WriteByte(byte value) => Write(value);

        public void WriteBytes(byte[] values)
        {
            foreach (var value in values)
                Write(value);
        }

        public void WriteShort(short value)
        {
            if (EndianType == EndianType.Little)
            {
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
            }
            else
            {
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)(value & 0xFF));
            }
        }

        public void WriteUShort(ushort value)
        {
            if (EndianType == EndianType.Little)
            {
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
            }
            else
            {
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)(value & 0xFF));
            }
        }

        public void WriteInt(int value)
        {
            if (EndianType == EndianType.Little)
            {
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
            }
            else
            {
                Write((byte)((value >> 24) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)(value & 0xFF));
            }
        }

        public void WriteUInt(uint value)
        {
            if (EndianType == EndianType.Little)
            {
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
            }
            else
            {
                Write((byte)((value >> 24) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)(value & 0xFF));
            }
        }

        public void WriteLong(long value)
        {
            if (EndianType == EndianType.Little)
            {
                for (int i = 0; i < 8; i++)
                    Write((byte)((value >> (8 * i)) & 0xFF));
            }
            else
            {
                for (int i = 7; i >= 0; i--)
                    Write((byte)((value >> (8 * i)) & 0xFF));
            }
        }

        public void WriteBool(bool value) => Write(value ? (byte)1 : (byte)0);

        public void WriteString(string value)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(value);
            WriteBytes(bytes);
            WriteByte(0x00);
        }

        public void WriteFloat(float value)
        {
            int val = BitConverter.SingleToInt32Bits(value);
    
            Span<byte> span = stackalloc byte[4];
            if (EndianType == EndianType.Little)
                BinaryPrimitives.WriteInt32LittleEndian(span, val);
            else
                BinaryPrimitives.WriteInt32BigEndian(span, val);
            
            WriteBytes(span.ToArray()); 
        }

        public void WriteDouble(double value)
        {
            long val = BitConverter.DoubleToInt64Bits(value);
    
            Span<byte> span = stackalloc byte[8];
            if (EndianType == EndianType.Little)
                BinaryPrimitives.WriteInt64LittleEndian(span, val);
            else
                BinaryPrimitives.WriteInt64BigEndian(span, val);

            WriteBytes(span.ToArray());
        }
    }
}