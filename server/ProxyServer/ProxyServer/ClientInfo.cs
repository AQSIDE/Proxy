using System.Collections.Concurrent;
using System.Net;

namespace ProxyServer;

public class ClientInfo
{
    public readonly ConcurrentDictionary<ProxySession, byte> Sessions = new();
    public string Login { get; set; }
    public IPEndPoint EndPoint { get; set; }
    
    public int ActiveConnections => Sessions.Count;
    public long BytesReceived;
    public long BytesSent;
    
    public DateTime LastActive { get; set; }

    public ClientInfo(string login, IPEndPoint endPoint)
    {
        Login = login;
        EndPoint = endPoint;
        BytesReceived = 0;
        BytesSent = 0;
        
        LastActive = DateTime.UtcNow;
    }
    
    public void KillAll()
    {
        foreach (var session in Sessions.Keys)
        {
            session.Close();
        }
    }
}