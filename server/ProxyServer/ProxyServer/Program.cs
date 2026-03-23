using ProxyServer;

public class Program
{
    static async Task Main(string[] args)
    {
        var proxy = new Proxy(args.Length > 0 ? int.Parse(args[0]) : 8888);
        proxy.Start();
        
        await Task.Delay(-1);
    }
}