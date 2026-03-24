using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyServer;

public class Proxy
{
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

    private readonly TcpListener _proxyListener;
    private readonly TcpListener _clientsListener;

    private readonly ProxySettings _settings;

    public ProxyStatistics Statistics { get; } = new();
    public int Connections => _clients.Count;
    public IReadOnlyList<ClientInfo> Clients => _clients.Values.ToList().AsReadOnly();

    public Proxy(int port, ProxySettings settings)
    {
        _proxyListener = new TcpListener(IPAddress.Any, port);
        _clientsListener = new TcpListener(IPAddress.Any, 9999);

        Statistics.Port = port;
        Statistics.StartTime = DateTime.UtcNow;

        _settings = settings;
    }

    public void Start()
    {
        _proxyListener.Start();
        _clientsListener.Start();

        //_ = HandleClientLoop();
        _ = HandleProxyLoop();

        //Logger.Log($"PROXY SERVER START LISTENING", ConsoleColor.Green);
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
            client.ReceiveTimeout = _settings.TimeoutMs;

            _ = HandleProxy(client);
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        try
        {
            //Logger.Log($"NEW CLIENT: {client.Client.RemoteEndPoint}", ConsoleColor.Green);

            //var stream = client.GetStream();
            //var clnt = new ClientInfo(stream);

            //clnt.Reader.PacketReceived += HandlePacketReceived;


            //Logger.Log($"CLIENT DISCONNECTED: {client.Client.RemoteEndPoint}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            //Logger.Log($"[ERROR] CLIENT ERROR: {ex.Message}", ConsoleColor.Red);
        }
    }

    private void HandlePacketReceived(byte[] packet)
    {
    }

    private async Task HandleProxy(TcpClient client)
    {
        var clientStream = client.GetStream();
        var buffer = new byte[_settings.BufferSize];
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

        var login = ctx.Value.Login;
        var password = ctx.Value.Password;
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            var response = Response.GetResponseBytes(Response.AUTH_REQUIRED);
            await clientStream.WriteAsync(response, 0, response.Length);

            //Logger.Log($"[AUTH] SENT 407 AUTH REQUEST TO CLIENT", ConsoleColor.Cyan);

            client.Close();
            return;
        }

        var allowed = _settings.AllowedConnections.FirstOrDefault(u => u.Login == login && u.Password == password);
        if (allowed == null)
        {
            client.Close();
            clientStream.Close();
            //Logger.Log($"[AUTH] UNKNOWN CONNECTION: {login}:{password}", ConsoleColor.Red);
            return;
        }

        if (!_clients.TryGetValue(login, out var clientInfo))
        {
            clientInfo = new ClientInfo(login, (IPEndPoint)client.Client.RemoteEndPoint);
            _clients[login] = clientInfo;
        }

        clientInfo.LastActive = DateTime.UtcNow;
        if (clientInfo.ActiveConnections >= allowed.MaxConnections)
        {
            client.Close();
            clientStream.Close();

            //Logger.Log($"[LIMIT] User '{login}' exceeded limit ({allowed.MaxConnections})", ConsoleColor.Magenta);
            return;
        }
        
        var host = ctx.Value.Host;
        var port = int.Parse(ctx.Value.Port);
        var method = ctx.Value.Method;
        
        ProxySession? session = null;
        try
        {
            //Logger.Log($"[CONN] CONNECTING TO: {host}:{port}, METHOD={method.ToString()}", ConsoleColor.Yellow);

            var server = new TcpClient();
            await server.ConnectAsync(host, port);
            var serverStream = server.GetStream();

            if (method == ProxyMethod.CONNECT)
            {
                var response = Response.GetResponseBytes(Response.CONN_ESTABLISHED);
                await clientStream.WriteAsync(response);
            }
            else
            {
                await serverStream.WriteAsync(buffer, 0, bytesRead);
            }

            session = new ProxySession(host, client, server, clientStream, serverStream);
            clientInfo.Sessions.TryAdd(session, 0);

            var clientToServer = Relay(clientInfo, clientStream, serverStream, Direction.CLIENT_TO_HOST);
            var serverToClient = Relay(clientInfo, serverStream, clientStream, Direction.HOST_TO_CLIENT);

            await Task.WhenAny(clientToServer, serverToClient);
        }
        catch (Exception ex) { }
        finally
        {
            clientInfo.Sessions.TryRemove(session, out _);
            session?.Close();
            
            var remaining = clientInfo.ActiveConnections;
            if (remaining <= 0)
            {
                _clients.TryRemove(login, out _);
            }
        }
    }

    private async Task Relay(ClientInfo client, NetworkStream input, NetworkStream output, Direction direction)
    {
        var relayBuffer = new byte[_settings.BufferSize];
        try
        {
            while (true)
            {
                using var cts = new CancellationTokenSource(_settings.TimeoutMs);
                
                var bytes = await input.ReadAsync(relayBuffer, 0, relayBuffer.Length, cts.Token);
                if (bytes == 0) break;

                await output.WriteAsync(relayBuffer, 0, bytes, cts.Token);

                if (direction == Direction.CLIENT_TO_HOST)
                {
                    Interlocked.Add(ref client.BytesSent, bytes);
                    Interlocked.Add(ref Statistics.Sent, bytes);
                }
                else
                {
                    Interlocked.Add(ref client.BytesReceived, bytes);
                    Interlocked.Add(ref Statistics.Received, bytes);
                }
            }
        }
        catch
        {
        }
    }
}

public enum Direction
{
    CLIENT_TO_HOST,
    HOST_TO_CLIENT,
}