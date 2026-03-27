namespace ProxyServer;

public class ProxySettings
{
    public List<AllowedConnection> AllowedConnections { get; init; }
    public int TimeoutSec { get; init; } = 300;
    public int HandshakeBufferSize { get; init; }
    public int RelayBufferSize { get; init; }
}

public class AllowedConnection
{
    public string Login { get; init; }
    public string Password { get; init; }
    public int MaxConnections { get; init; } = 50;
    public int MaxIPs { get; init; } = 2;
}