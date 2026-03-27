using ProxyServer.Routing.Default;

namespace ProxyServer.Routing.Types;

public class ConfigHandler : ChoiceHandler
{
    protected override async Task<bool> OnSelected(string? text, UserContext ctx)
    {
        switch (text)
        {
            case "0":
                await ctx.Router.Route(new MainMenuHandler());
                return false;
            case "1":
                var settings = ctx.Server.SettingsLoader.Load();
                ctx.Server.LoadSettings(settings);
                
                await ctx.Router.Route(new ConfigHandler());
                return false;
        }
        
        return true;
    }

    protected override void WriteHeader(UserContext ctx)
    {
        var config = ctx.Server.Settings;
        
        Console.Clear();
        Logger.Log($"CONFIG DETAILS");
        Logger.Log($"PATH: {AppDomain.CurrentDomain.BaseDirectory}");
        Logger.Log("---------------------------");
        Logger.Log($"[BUFFERS] Relay: {config.RelayBufferSize / 1024} KB | Handshake: {config.HandshakeBufferSize / 1024} KB");
        Logger.Log($"[TIMEOUT] Idle: {config.TimeoutSec} seconds");
        Logger.Log("---------------------------");
        Logger.Log("ALLOWED TARIFFS:");
        foreach (var conn in config.AllowedConnections)
        {
            Logger.Log($" > {conn.Login,-10} | Limit: {conn.MaxConnections,-4} | Max IPs: {conn.MaxIPs}");
        }
        Logger.Log("---------------------------");
        Logger.Log("");
        Logger.Log("1 - Reload");
        Logger.Log("0 - Back");
        Logger.Log("");
        Logger.Log(">>> ", newLine:false);
    }
}