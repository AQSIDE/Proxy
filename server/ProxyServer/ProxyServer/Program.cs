using ProxyServer;
using ProxyServer.Routing;
using ProxyServer.Routing.Types;

public class Program
{
    static async Task Main(string[] args)
    {
        var setting = new ProxySettings
        {
            RelayBufferSize = 65536,
            HandshakeBufferSize = 8192,
            TimeoutSec = 300,
            AllowedConnections = new List<AllowedConnection>
            {
                new AllowedConnection { Login = "root30", Password = "password", MaxConnections = 30},
                new AllowedConnection { Login = "root50", Password = "password", MaxConnections = 50},
                new AllowedConnection { Login = "root80", Password = "password", MaxConnections = 80},
                new AllowedConnection { Login = "root90", Password = "password", MaxConnections = 90},
                new AllowedConnection { Login = "root100", Password = "password", MaxConnections = 100},
                new AllowedConnection { Login = "root150", Password = "password", MaxConnections = 150},
                new AllowedConnection { Login = "root200", Password = "password", MaxConnections = 200},
            }
        };

        var port = args.Length > 0 ? int.Parse(args[0]) : 8888;
        var useDebug = args.Length > 1 ? bool.Parse(args[1]) : false;
        
        var proxy = new Proxy(port, setting, useDebug);
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