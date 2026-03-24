using System.Net.Sockets;

namespace ProxyServer;

public class ProxySession
{
    public string Host { get;}
    public TcpClient Client { get; }
    public TcpClient Server { get; }
    public NetworkStream? ClientStream { get; }
    public NetworkStream? ServerStream { get; }
    public DateTime ConnectedAt { get; }

    public ProxySession(string host, TcpClient client, TcpClient server, NetworkStream clientStream, NetworkStream serverStream)
    {
        this.Host = host;
        this.Client = client;
        this.Server = server;
        this.ClientStream = clientStream;
        this.ServerStream = serverStream;
        ConnectedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Client?.Close();
        Server?.Close();
        ClientStream?.Close();
        ServerStream?.Close();
    }
}