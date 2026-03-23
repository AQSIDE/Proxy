using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyServer;

public class Proxy
{
    private readonly List<Client> _clients = new();
    
    private readonly TcpListener _proxyListener;
    private readonly TcpListener _clientsListener;

    public Proxy(int port)
    {
        _proxyListener = new TcpListener(IPAddress.Loopback, port);
        _clientsListener = new TcpListener(IPAddress.Loopback, 9999);
    }

    public void Start()
    {
        _proxyListener.Start();
        _clientsListener.Start();
        
        _ = HandleClientLoop();
        _ = HandleProxyLoop();
        
        Logger.Log($"PROXY SERVER START LISTENING", ConsoleColor.Green);
    }
    
    private async Task HandleClientLoop()
    {
        while (true)
        {
            var client = await _clientsListener.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    private async Task HandleProxyLoop()
    {
        while (true)
        {
            var client = await _proxyListener.AcceptTcpClientAsync();
            _ = HandleProxy(client);
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        try
        {
            Logger.Log($"NEW CLIENT: {client.Client.RemoteEndPoint}", ConsoleColor.Green);
            
            var stream = client.GetStream();
            var clnt = new Client(stream);

            clnt.Reader.PacketReceived += HandlePacketReceived;
            
            _clients.Add(clnt);
            
            while (client.Connected)
            {
            }
            
            _clients.Remove(clnt);
            client.Close();
            stream.Close();
            
            Logger.Log($"CLIENT DISCONNECTED: {client.Client.RemoteEndPoint}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            Logger.Log($"[ERROR] CLIENT ERROR: {ex.Message}", ConsoleColor.Red);
        }
    }

    private void HandlePacketReceived(byte[] packet)
    {
        
    }

    private async Task HandleProxy(TcpClient client)
    {
        try
        {
            var clientStream = client.GetStream();
            var buffer = new byte[8192];
            var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                client.Close();
                clientStream.Close();
                return;
            }
            
            var request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            var ctx = Parser.GetUrl(request);
            if (ctx == null)
            {
                client.Close();
                clientStream.Close();
                return;
            }

            var host = ctx.Value.Host;
            var port = int.Parse(ctx.Value.Port);
            var method = ctx.Value.Method;
            
            Logger.Log($"[CONN] CONNECTING TO: {host}:{port}, METHOD={method.ToString()}", ConsoleColor.Yellow);

            var server = new TcpClient();
            await server.ConnectAsync(host, port);
            var serverStream = server.GetStream();

            if (method == ProxyMethod.CONNECT)
            {
                var ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                await clientStream.WriteAsync(ok);
            }
            else
            {
                await serverStream.WriteAsync(buffer, 0, bytesRead);
            }

            var clientToServer = Relay(clientStream, serverStream, $"[CLIENT -> {host}]");
            var serverToClient = Relay(serverStream, clientStream, $"[{host} -> CLIENT]");

            await Task.WhenAny(clientToServer, serverToClient);
            
            Logger.Log($"[FIN] CONNECTION TORN: {host}", ConsoleColor.Red);
            
            // Closing stream
            clientStream.Close();
            serverStream.Close();
            
            client.Close();
            server.Close();
        }
        catch (Exception ex)
        {
            Logger.Log($"[ERROR] PROCESSING ERROR: {ex.Message}", ConsoleColor.Red);
        }
    }

    private async Task Relay(NetworkStream input, NetworkStream output, string direction)
    {
        byte[] relayBuffer = new byte[8192];
        try
        {
            while (true)
            {
                var bytes = await input.ReadAsync(relayBuffer, 0, relayBuffer.Length);
                if (bytes == 0) break;

                await output.WriteAsync(relayBuffer, 0, bytes);

                Logger.Log($"{direction}: {bytes} BYTES TRANSFERRED", ConsoleColor.Green);
            }
        }
        catch (Exception ex) { }
    }
}