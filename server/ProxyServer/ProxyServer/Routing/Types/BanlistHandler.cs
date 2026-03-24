using ProxyServer.Routing.Default;

namespace ProxyServer.Routing.Types;

public class BanlistHandler : ChoiceHandler
{
    protected override async Task<bool> OnSelected(string? text, UserContext ctx)
    {
        switch (text)
        {
            case "0":
                await ctx.Router.Route(new MainMenuHandler());
                return false;
            case "1":
                await ctx.Router.Route(new ConnectionsHandler());
                return false;
        }
        
        return true;
    }

    protected override void WriteHeader(UserContext ctx)
    {
        var blacklist = ctx.Server.Blacklist;
        
        Console.Clear();
        Logger.Log("PROXY BLACKLIST");
        Logger.Log("---------------------------");
        if (blacklist.Count > 0)
        {
            for (var i = 0; i < blacklist.Count; i++)
            {
                var b = blacklist[i];
                var timeLeft = b.Until - DateTime.UtcNow;
                
                var timeStatus = timeLeft.TotalSeconds > 0 
                    ? $"{(int)timeLeft.TotalMinutes}m {timeLeft.Seconds}s left" 
                    : "EXPIRED";

                Logger.Log($"{i + 1}. {b.IP} | Until: {b.Until.ToLocalTime():HH:mm:ss} ({timeStatus})");
            }
        }
        else
        {
            Logger.Log("Blacklist is empty.");
        }
        Logger.Log("---------------------------");
        Logger.Log("");
        Logger.Log("0 - Back");
        Logger.Log("");
        Logger.Log(">>> ", newLine:false);
    }
}