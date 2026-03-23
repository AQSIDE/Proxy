using ProxyServer.Buffer;

namespace ProxyServer.Packets;

public enum PacketType
{
    CONNECT = 1,
    CLOSE = 2,
    TRAFFIC = 3,
}

public interface IPacket
{
    PacketType Type { get; }
    
    void Read(ReadBuffer buffer);
    void Write(WriteBuffer buffer);
}

public class ConnectPacket : IPacket
{
    public PacketType Type => PacketType.CONNECT;
    public string Magic { get; private set; }
    
    public void Read(ReadBuffer buffer)
    {
        Magic = buffer.ReadString();
    }

    public void Write(WriteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}