namespace ProxyServer;

public class ProxySettings
{
    public List<AllowedConnection> AllowedConnections { get; init; } = new();
    public int TimeoutSec { get; init; } = 300;
    public int HandshakeBufferSize { get; init; } = 4095;
    public int RelayBufferSize { get; init; } = 4096;
}

public class AllowedConnection
{
    public string Login { get; init; }
    public string Password { get; init; }
    public int MaxConnections { get; init; } = 200;
    public int MaxIPs { get; init; } = 2;
}