using System.Net.Sockets;
using ProxyServer.Tcp;

namespace ProxyServer;

public class Client
{
    public TcpReadService Reader { get; }
    public TcpWriteService Writer { get; }

    public Client(NetworkStream stream)
    {
        Reader = new TcpReadService(stream);
        Writer = new TcpWriteService(stream);
        
        Reader.Start();
        Writer.Start();
    }
}