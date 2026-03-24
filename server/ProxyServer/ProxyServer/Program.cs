using ProxyServer;
using ProxyServer.Routing;
using ProxyServer.Routing.Types;

public class Program
{
    static async Task Main(string[] args)
    {
        var setting = new ProxySettings
        {
            BufferSize = 8124,
            AllowedConnections = new List<AllowedConnection>
            {
                new AllowedConnection { Login = "root", Password = "root", MaxConnections = 30},
                new AllowedConnection { Login = "admin", Password = "password123", MaxConnections = 50 }
            }
        };
        
        var proxy = new Proxy(args.Length > 0 ? int.Parse(args[0]) : 8888, setting);
        proxy.Start();

        var user = new UserContext(proxy);
        await user.Router.Route(new MainMenuHandler());
    }
}