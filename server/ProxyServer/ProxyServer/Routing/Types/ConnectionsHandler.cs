using ProxyServer.Routing.Default;

namespace ProxyServer.Routing.Types;

public class ConnectionsHandler : ChoiceHandler
{
    protected override async Task<bool> OnSelected(string? text, UserContext ctx)
    {
        switch (text)
        {
            case "0":
                await ctx.Router.Route(new MainMenuHandler());
                return false;
        }

        if (int.TryParse(text, out var index))
        {
            var currentClients = ctx.Server.Clients; 
            var actualIndex = index - 1;

            if (actualIndex >= 0 && actualIndex < currentClients.Count)
            {
                ctx.SelectedConnectionId = actualIndex;
                
                await ctx.Router.Route(new ConnHandler());
                return false;
            }
        }
        
        return true;
    }

    protected override void WriteHeader(UserContext ctx)
    {
        Console.Clear();
        Logger.Log("ACTIVE CONNECTIONS");
        Logger.Log("---------------------------");
        var clients = ctx.Server.Clients;
        if (clients.Count == 0)
        {
            Logger.Log("No active connections.");
        }
        else
        {
            for (var i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                var traffic = $"{ctx.Server.Statistics.GetFormattedSize(client.BytesSent)} / {ctx.Server.Statistics.GetFormattedSize(client.BytesReceived)}";
            
                Logger.Log($"> {i + 1, -2} | {client.Login,-5} | {client.EndPoint.ToString(),-15} | {client.ActiveConnections} | {traffic}");
            }
        }
        Logger.Log("---------------------------");
        Logger.Log("");
        Logger.Log("[ID] - Select Connection");
        Logger.Log("0 - Back");
        Logger.Log("");
        Logger.Log(">>> ", newLine:false);
    }
}