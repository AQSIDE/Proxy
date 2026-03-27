using System.Net.Sockets;

namespace ProxyServer;

public class ProxySession
{
    public ProtocolType ProtocolType { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public int MaxConnections { get; set; }
    public string Login { get; set; }
    public TcpClient Client { get; set; }
    public TcpClient Server { get; set; }
    public NetworkStream? ClientStream { get; set; }
    public NetworkStream? ServerStream { get; set; }
    public DateTime ConnectedAt { get; set; }
    public CancellationTokenSource Cts { get; }

    public ProxySession(CancellationTokenSource cts)
    {
        this.Cts = cts;
        ConnectedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        try { Cts?.Cancel(); } catch {}
        
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