namespace ProxyServer.Routing;

public abstract class RoutingHandler
{
    public abstract Task Handle(UserContext ctx);
    public virtual Task OnExit(UserContext ctx) { return Task.CompletedTask; }
}