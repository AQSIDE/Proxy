using System.Text;

namespace ProxyServer;

public static class Parser
{
    public static UrlContext? GetUrl(string request)
    {
        var firstLine = request.Split('\n')[0].Trim();
        var parts = firstLine.Split(' ');
        if (parts.Length < 2) return null;

        var method = ProxyMethod.UNKNOWN;
        var host = new StringBuilder();
        var port = new StringBuilder();
        
        if (parts[0] == "CONNECT")
        {
            method = ProxyMethod.CONNECT;
            
            var address = parts[1];
            TrimAddress(ref address);
            ParseAddress(address, host, port);
        }
        else if (parts[0] == "GET")
        {
            method = ProxyMethod.GET;
            
            var address = parts[1];
            TrimAddress(ref address);
            ParseAddress(address, host, port);
        }
        else if (parts[0] == "POST")
        {
            method = ProxyMethod.POST; 
            
            var address = parts[1];
            TrimAddress(ref address);
            ParseAddress(address, host, port);
        }
        
        var finalHost = host.ToString();
        var finalPort = port.ToString();

        if (string.IsNullOrEmpty(finalPort))
        {
            finalPort = (method == ProxyMethod.CONNECT) ? "443" : "80";
        }

        return new UrlContext(finalHost, finalPort, method);
    }

    private static void TrimAddress(ref string address)
    {
        if (address.StartsWith("http://")) address = address.Substring(7);
        else if (address.StartsWith("https://")) address = address.Substring(8);
        else if (address.StartsWith("//")) address = address.Substring(2);
    }
    
    private static void ParseAddress(string address, StringBuilder host, StringBuilder port)
    {
        var isPort = false;
        for (var i = 0; i < address.Length; i++)
        {
            var c = address[i];
            if (c == '/') break;
            
            if (c == ':')
            {
                isPort = true;
                continue;
            }

            if (isPort) port.Append(c);
            else host.Append(c);
        }
    }
}

public readonly struct UrlContext
{
    public readonly string Host;
    public readonly string Port;
    public readonly ProxyMethod Method;

    public UrlContext(string host, string port, ProxyMethod method)
    {
        Host = host;
        Port = port;
        Method = method;
    }

    public override string ToString()
    {
        return $"Address: {Host}:{Port} Method: {Method.ToString()}";
    }
}

public enum ProxyMethod
{
    UNKNOWN,
    CONNECT, // HTTPS
    GET,     // HTTP
    POST     // HTTP
}