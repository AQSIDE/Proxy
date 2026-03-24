namespace ProxyServer.Routing.Default;

public abstract class ChoiceHandler : RoutingHandler
{
    public override async Task Handle(UserContext ctx)
    {
        while (true)
        {
            WriteHeader(ctx);
            var text = Console.ReadLine();
            
            if (!await OnSelected(text, ctx)) break;
        }
    }

    protected abstract Task<bool> OnSelected(string? text, UserContext ctx);
    protected abstract void WriteHeader(UserContext ctx);
}