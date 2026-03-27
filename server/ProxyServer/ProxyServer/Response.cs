using System.Text;

namespace ProxyServer;

public static class Response
{
    public const string AUTH_REQUIRED_HTTP = "HTTP/1.1 407 Proxy Authentication Required\r\n" +
        "Proxy-Authenticate: Basic realm=\"ProxyServer\"\r\n" + 
        "Content-Length: 0\r\n" +
        "Connection: close\r\n\r\n";
    
    public const string CONN_ESTABLISHED = "HTTP/1.1 200 Connection Established\r\n\r\n";
    
    public static readonly byte[] FAILURE_SOCKS5 = new byte[] { 0x01, 0x01 };
    public static readonly byte[] AUTH_SOCKS5 = new byte[] { 0x05, 0x02 };
    public static readonly byte[] SUCCESS_SOCKS5 = new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
    public static readonly byte[] HOST_UNREACHABLE_SOCKS5 = { 0x05, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

    public static byte[] GetResponseBytes(string request)
    {
        return Encoding.ASCII.GetBytes(request);
    }
}