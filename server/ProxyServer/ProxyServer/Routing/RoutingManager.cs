namespace ProxyServer.Routing;

public class RoutingManager
{
    public RoutingHandler? CurrentRouter { get; private set; }
    public UserContext UserContext { get; }

    public RoutingManager(UserContext userContext)
    {
        UserContext = userContext;
    }
    
    public async Task Route(RoutingHandler router)
    {
        if (CurrentRouter != null)
            await CurrentRouter.OnExit(UserContext);
        
        CurrentRouter = router;
        await CurrentRouter.Handle(UserContext);
    }
}