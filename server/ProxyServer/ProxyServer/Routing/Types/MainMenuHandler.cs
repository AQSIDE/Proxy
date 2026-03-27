using System.Net;
using ProxyServer.Routing.Default;

namespace ProxyServer.Routing.Types;

public class MainMenuHandler : ChoiceHandler
{
    protected override async Task<bool> OnSelected(string? text, UserContext ctx)
    {
        switch (text)
        {
            case "0":
                Console.Clear();
                Logger.Log("ARE YOU SURE WANT TO EXIT?", ConsoleColor.Red);
                Logger.Log("");
                Logger.Log("(y/n): ", newLine:false);
                var choice = Console.ReadLine();
                if (string.Equals(choice, "y", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                    return false;
                }
                
                return true;
            case "1":
                await ctx.Router.Route(new ConnectionsHandler());
                return false;
            case "2":
                await ctx.Router.Route(new BanlistHandler());
                return false;
            case "3":
                await ctx.Router.Route(new ConfigHandler());
                return false;
        }
        
        return true;
    }

    protected override void WriteHeader(UserContext ctx)
    {
        var stats = ctx.Server.Statistics;
        
        var localIPs = Dns.GetHostEntry(Dns.GetHostName()).AddressList
            .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(ip => ip.ToString());

        Console.Clear();
        Logger.Log("PROXY MAIN MENU");
        Logger.Log("---------------------------");
        Logger.Log($"Uptime: {stats.Uptime:dd\\.hh\\:mm\\:ss} | Connections: {ctx.Server.Connections}");
        
        Logger.Log($"Listening on: {string.Join(", ", localIPs)}:{stats.Port}");
    
        Logger.Log($"Traffic: Out: {stats.GetFormattedSize(stats.Sent)} / In: {stats.GetFormattedSize(stats.Received)}");
        Logger.Log("---------------------------");
        Logger.Log("");
        Logger.Log("1 - Connections");
        Logger.Log("2 - Blacklist");
        Logger.Log("3 - Config");
        Logger.Log("0 - Exit");
        Logger.Log("");
        Logger.Log(">>> ", newLine: false);
    }
}