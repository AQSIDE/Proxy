using System.Buffers.Binary;

namespace ProxyServer.Buffer
{
    public class ReadBuffer
    {
        private readonly byte[] _buffer;
        private int _offset;

        public EndianType EndianType { get; set; } = EndianType.Little;

        public int Length => _buffer.Length;
        public int ByteLeft => _buffer.Length - _offset;

        public ReadBuffer(byte[] buffer, int offset = 0)
        {
            _buffer = buffer;
            _offset = offset;
        }

        public byte[] ToArray() => _buffer;

        private byte GetValue()
        {
            if (_offset >= _buffer.Length)
                throw new InvalidOperationException("Read past buffer length");

            return _buffer[_offset++];
        }

        public byte ReadByte() => GetValue();

        public byte[] ReadBytes(int length)
        {
            var value = new byte[length];
            for (var i = 0; i < length; i++) value[i] = GetValue();
            return value;
        }

        public short ReadShort()
        {
            var b0 = GetValue();
            var b1 = GetValue();
            return EndianType == EndianType.Little
                ? (short)(b0 | (b1 << 8))
                : (short)((b0 << 8) | b1);
        }

        public ushort ReadUShort()
        {
            var b0 = GetValue();
            var b1 = GetValue();
            return EndianType == EndianType.Little
                ? (ushort)(b0 | (b1 << 8))
                : (ushort)((b0 << 8) | b1);
        }

        public int ReadInt()
        {
            var b0 = GetValue();
            var b1 = GetValue();
            var b2 = GetValue();
            var b3 = GetValue();
            return EndianType == EndianType.Little
                ? b0 | (b1 << 8) | (b2 << 16) | (b3 << 24)
                : (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        public uint ReadUInt()
        {
            var b0 = GetValue();
            var b1 = GetValue();
            var b2 = GetValue();
            var b3 = GetValue();
            return EndianType == EndianType.Little
                ? (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24))
                : (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
        }

        public long ReadLong()
        {
            long b0 = GetValue();
            long b1 = GetValue();
            long b2 = GetValue();
            long b3 = GetValue();
            long b4 = GetValue();
            long b5 = GetValue();
            long b6 = GetValue();
            long b7 = GetValue();

            return EndianType == EndianType.Little
                ? b0 | (b1 << 8) | (b2 << 16) | (b3 << 24) |
                  (b4 << 32) | (b5 << 40) | (b6 << 48) | (b7 << 56)
                : (b0 << 56) | (b1 << 48) | (b2 << 40) | (b3 << 32) |
                  (b4 << 24) | (b5 << 16) | (b6 << 8) | b7;
        }

        public string ReadString()
        {
            var bytes = new List<byte>();
            byte b;

            while ((b = ReadByte()) != 0)
            {
                bytes.Add(b);
            }

            return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
        }
        
        public string ReadString(int length)
        {
            if (length <= 0) return string.Empty;
    
            var buffer = new byte[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = ReadByte();
            }
            
            return System.Text.Encoding.UTF8.GetString(buffer);
        }

        public bool ReadBool() => GetValue() != 0;

        public float ReadFloat()
        {
            int val = EndianType == EndianType.Little
                ? BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_offset, 4))
                : BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(_offset, 4));
    
            _offset += 4;
            
            return BitConverter.Int32BitsToSingle(val);
        }

        public double ReadDouble()
        {
            long val = EndianType == EndianType.Little
                ? BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_offset, 8))
                : BinaryPrimitives.ReadInt64BigEndian(_buffer.AsSpan(_offset, 8));
    
            _offset += 8;
            
            return BitConverter.Int64BitsToDouble(val);
        }
    }
}