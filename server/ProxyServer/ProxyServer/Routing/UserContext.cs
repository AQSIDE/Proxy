namespace ProxyServer.Routing;

public class UserContext
{
    public int SelectedConnectionId { get; set; } = -1;
    public RoutingManager Router { get; }
    public Proxy Server { get; }

    public UserContext(Proxy server)
    {
        Router = new RoutingManager(this);
        Server = server;
    }
}