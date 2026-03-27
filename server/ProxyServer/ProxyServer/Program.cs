using ProxyServer;
using ProxyServer.Routing;
using ProxyServer.Routing.Types;

public class Program
{
    static async Task Main(string[] args)
    {
        var loader = new ProxySettingsLoader();
        var setting  = loader.Load();

        var port = args.Length > 0 ? int.Parse(args[0]) : 8888;
        var useDebug = args.Length > 1 ? bool.Parse(args[1]) : false;
        
        var proxy = new Proxy(loader, port, useDebug);
        proxy.LoadSettings(setting);
        proxy.Start();

        if (!useDebug)
        {
            var user = new UserContext(proxy);
            await user.Router.Route(new MainMenuHandler());
        }
        else
        {
            await Task.Delay(-1);
        }
    }
}