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
    public CancellationTokenSource Cts { get; }

    public ProxySession(string host, TcpClient client, TcpClient server, NetworkStream clientStream, NetworkStream serverStream, CancellationTokenSource cts)
    {
        this.Host = host;
        this.Client = client;
        this.Server = server;
        this.ClientStream = clientStream;
        this.ServerStream = serverStream;
        this.Cts = cts;
        ConnectedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        try { Cts?.Cancel(); } catch { /* ignore */ }
        
        ClientStream?.Close();
        ServerStream?.Close();
        
        Client?.Close();
        Server?.Close();
        
        Cts?.Dispose();
        ClientStream?.Dispose();
        ServerStream?.Dispose();
        Client?.Dispose();
        Server?.Dispose();
    }
}