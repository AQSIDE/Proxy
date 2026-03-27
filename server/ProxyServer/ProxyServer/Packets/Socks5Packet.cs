using ProxyServer.Buffer;

namespace ProxyServer.Packets;

public class Socks5AuthPacket
{
    public string Login { get; private set; }
    public string Password { get; private set; }

    public void Read(ReadBuffer buffer)
    {
        var ver = buffer.ReadByte();
        if (ver != 0x01)
        {
            throw new Exception("Invalid SOCKS5 Auth version");
        }

        int loginLen = buffer.ReadByte();
        Login = buffer.ReadString(loginLen);

        int passwordLen = buffer.ReadByte();
        Password = buffer.ReadString(passwordLen);
    }
}

public class Socks5RequestPacket
{
    public string Host { get; private set; }
    public int Port { get; private set; }

    public void Read(ReadBuffer buffer)
    {
        buffer.ReadByte(); // VER SOCKS (0x05)
        buffer.ReadByte(); // (0x01 - CONNECT)
        buffer.ReadByte(); // (0x00)
        
        var addressType = buffer.ReadByte(); // 0x01-IPv4, 0x04-IPv6, 0x03-Domain
        
        if (addressType == 0x01) // IPv4
        {
            var b1 = buffer.ReadByte();
            var b2 = buffer.ReadByte();
            var b3 = buffer.ReadByte();
            var b4 = buffer.ReadByte();
            Host = $"{b1}.{b2}.{b3}.{b4}";
        }
        else if (addressType == 0x04) // IPv6
        {
            var bytes = buffer.ReadBytes(16);
            Host = new System.Net.IPAddress(bytes).ToString();
        }
        else if (addressType == 0x03) // Domain Name
        {
            int hostLen = buffer.ReadByte();
            Host = buffer.ReadString(hostLen);
        }
        else 
        {
            throw new Exception($"Unsupported address type: {addressType}");
        }

        // (Big-Endian)
        Port = buffer.ReadUShort();
    }
}