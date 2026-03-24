using ProxyServer.Routing.Default;

namespace ProxyServer.Routing.Types;

public class MainMenuHandler : ChoiceHandler
{
    protected override async Task<bool> OnSelected(string? text, UserContext ctx)
    {
        switch (text)
        {
            case "0":
                Environment.Exit(0);
                return false;
            case "1":
                await ctx.Router.Route(new MainMenuHandler());
                return false;
            case "2":
                await ctx.Router.Route(new ConnectionsHandler());
                return false;
        }
        
        return true;
    }

    protected override void WriteHeader(UserContext ctx)
    {
        var stats = ctx.Server.Statistics;
        
        Console.Clear();
        Logger.Log("PROXY MAIN MENU");
        Logger.Log("---------------------------");
        Logger.Log($"Uptime: {stats.Uptime:dd\\.hh\\:mm\\:ss} | Connections: {ctx.Server.Connections}");
        Logger.Log($"Listening on: {stats.Port}");
        Logger.Log($"Traffic: Out: {stats.GetFormattedSize(stats.Sent)} / In: {stats.GetFormattedSize(stats.Received)}");
        Logger.Log("---------------------------");
        Logger.Log("");
        Logger.Log("1 - Refresh");
        Logger.Log("2 - Connections");
        Logger.Log("0 - Exit");
        Logger.Log("");
        Logger.Log(">>> ", newLine:false);
    }
}