using ProxyServer.Routing.Default;

namespace ProxyServer.Routing.Types;

public class ConnHandler : ChoiceHandler
{
    private static ClientInfo _client;
    
    protected override async Task<bool> OnSelected(string? text, UserContext ctx)
    {
        switch (text)
        {
            case "0":
                await ctx.Router.Route(new ConnectionsHandler());
                return false;
            case "1":
                ctx.Server.Kill(_client);
                
                Console.Clear();
                Logger.Log($"[!!!] CONNECTION TERMINATED for user: {_client.Login}", ConsoleColor.Red);
                Logger.Log("");
                Logger.Log("Press any key...");
    
                Console.ReadKey();
                
                await ctx.Router.Route(new ConnectionsHandler());
                return false;
            case "2":
                Console.Clear();
                Logger.Log($"--- BAN USER: {_client.Login} ---", ConsoleColor.Yellow);
                Logger.Log("Enter ban time in minutes (0 back):", ConsoleColor.White);
    
                if (!int.TryParse(Console.ReadLine(), out var minutes)) minutes = 0;
                if (minutes <= 0) 
                {
                    await ctx.Router.Route(new ConnHandler());
                    return false;
                }

                var ipStr = _client.EndPoint.Address.ToString();
                ctx.Server.Ban(_client, minutes);
                ctx.SelectedConnectionId = -1;

                Logger.Log($"[!!!] USER {_client.Login} BANNED for {minutes} min. (IP: {ipStr})", ConsoleColor.Red);

                Logger.Log("");
                Logger.Log("Press any key...");
                Console.ReadKey();
    
                await ctx.Router.Route(new ConnectionsHandler());
                return false;
        }
        
        return true;
    }

    protected override void WriteHeader(UserContext ctx)
    {
        _client = ctx.Server.Clients[ctx.SelectedConnectionId];
    
        Console.Clear();
        Logger.Log($"CONNECTION DETAILS: {_client.Login}");
        Logger.Log("---------------------------");
        Logger.Log($"Remote IP:    {_client.EndPoint}");
        Logger.Log($"Total Sent:   {ctx.Server.Statistics.GetFormattedSize(_client.BytesSent)}");
        Logger.Log($"Total Received: {ctx.Server.Statistics.GetFormattedSize(_client.BytesReceived)}");
        Logger.Log($"Last activity: {_client.LastActive:HH:mm:ss}");
        Logger.Log("---------------------------");
        
        var sessions = _client.Sessions.Keys.ToList();
        Logger.Log($"ACTIVE SESSIONS ({sessions.Count}):");
    
        if (sessions.Count == 0)
        {
            Logger.Log("  (No active host connections)");
        }
        else
        {
            foreach (var s in sessions)
            {
                var uptime = DateTime.UtcNow - s.ConnectedAt;
                Logger.Log($"> {s.Host,-30} [{uptime:hh\\:mm\\:ss}]");
            }
        }

        Logger.Log("---------------------------");
        Logger.Log("");
        Logger.Log("1 - Kill");
        Logger.Log("2 - Ban");
        Logger.Log("0 - Back");
        Logger.Log("");
        Logger.Log(">>> ", newLine:false);
    }
}