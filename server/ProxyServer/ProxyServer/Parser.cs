using System.Text;

namespace ProxyServer;

public static class Parser
{
    public static ConnectContext? GetContext(byte[] buffer, int bytesRead, ProtocolType protocol)
    {
        var finalHost = string.Empty;
        var finalPort = string.Empty;
        
        var login = string.Empty;
        var password = string.Empty;
        var method = ProxyMethod.UNKNOWN;
        
        if (protocol == ProtocolType.HTTP)
        {
            var request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            
            var firstLine = request.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Trim();
            var parts = firstLine.Split(' ');
            if (parts.Length < 2) return null;
            
            var host = new StringBuilder();
            var port = new StringBuilder();
        
            if (parts[0] == "CONNECT")
            {
                method = ProxyMethod.CONNECT;
            
                var address = parts[1];
                TrimAddress(ref address);
                ParseAddress(address, host, port);
            }
            else 
            {
                var address = parts[1];
                TrimAddress(ref address);
                ParseAddress(address, host, port);
            }

            GetProxyAuth(request, out login, out password);
            
            finalHost = host.ToString();
            finalPort = port.ToString();

            if (string.IsNullOrEmpty(finalPort))
            {
                finalPort = (method == ProxyMethod.CONNECT) ? "443" : "80";
            }
        }
        else
        {
            return null;
        }

        return new ConnectContext(finalHost, finalPort, login, password, method);
    }

    private static bool GetProxyAuth(string request, out string login, out string password)
    {
        var parameter = "Proxy-Authorization:";
        login = string.Empty;
        password = string.Empty;
        
        var index = request.IndexOf(parameter, StringComparison.OrdinalIgnoreCase);
        if (index == -1) return false;
        
        var start = index + parameter.Length;
        var end = request.IndexOf("\r\n", start);
        if (end == -1) end = request.Length;

        var headerValue = request.Substring(start, end - start).Trim();
        
        if (headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try 
            {
                var base64Credentials = headerValue.Substring(6).Trim();
                var data = Convert.FromBase64String(base64Credentials);
                var decoded = Encoding.UTF8.GetString(data);

                var colonIndex = decoded.IndexOf(':');
                if (colonIndex != -1)
                {
                    login = decoded.Substring(0, colonIndex);
                    password = decoded.Substring(colonIndex + 1);
                    return true;
                }
            }
            catch {}
        }

        return false;
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

public readonly struct ConnectContext
{
    public readonly string Login;
    public readonly string Password;
    
    public readonly string Host;
    public readonly string Port;
    
    public readonly ProxyMethod Method;

    public ConnectContext(string host, string port, string login, string password, ProxyMethod method)
    {
        Host = host;
        Port = port;
        Login = login;
        Password = password;
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
    CONNECT,
}