using System.Text;

namespace ProxyServer;

public static class Response
{
    public const string AUTH_REQUIRED = "HTTP/1.1 407 Proxy Authentication Required\r\n" +
        "Proxy-Authenticate: Basic realm=\"ProxyServer\"\r\n" + 
        "Content-Length: 0\r\n" +
        "Connection: close\r\n\r\n";
    
    public const string CONN_ESTABLISHED = "HTTP/1.1 200 Connection Established\r\n\r\n";

    public static byte[] GetResponseBytes(string request)
    {
        return Encoding.ASCII.GetBytes(request);
    }
}