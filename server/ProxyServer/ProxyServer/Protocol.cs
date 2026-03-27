using System.Net.Sockets;

namespace ProxyServer;

public static class Protocol
{
    public static ProtocolType GetProtocol(byte[] buffer)
    {
        if (buffer.Length == 0) return ProtocolType.UNKNOWN;
        var firstByte = buffer[0];

        if (firstByte == 0x05)
            return ProtocolType.SOCKS5;

        if (firstByte is (byte)'G' or (byte)'P' or (byte)'C' or (byte)'H' or (byte)'D' or (byte)'O')
            return ProtocolType.HTTP;

        return ProtocolType.UNKNOWN;
    }

    public static async Task<int> ReadFullPacket(NetworkStream stream, byte[] buffer, int alreadyRead,
        ProtocolType protocol)
    {
        var totalRead = alreadyRead;

        while (totalRead < buffer.Length)
        {
            if (IsPacketComplete(buffer, totalRead, protocol))
                return totalRead;

            var read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
            if (read <= 0) return totalRead;
            totalRead += read;
        }

        return totalRead;
    }
    
    private static bool IsPacketComplete(byte[] buffer, int totalRead, ProtocolType protocol)
    {
        if (protocol == ProtocolType.HTTP)
        {
            if (totalRead < 4) return false;
            
            // \r\n\r\n (0x0D 0x0A 0x0D 0x0A)
            for (var i = 0; i <= totalRead - 4; i++)
            {
                if (buffer[i] == (byte)'\r' && buffer[i + 1] == (byte)'\n' &&
                    buffer[i + 2] == (byte)'\r' && buffer[i + 3] == (byte)'\n')
                    return true;
            }
            return false;
        }
        
        if (protocol == ProtocolType.SOCKS5)
        {
            return totalRead >= 3; 
        }

        return true;
    }
}

public enum ProtocolType
{
    UNKNOWN,
    HTTP,
    SOCKS5,
}