using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProxyServer;

public class Proxy
{
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
    private readonly ConcurrentDictionary<string, BannedClient> _blackList = new();

    private readonly TcpListener _proxyListener;

    private readonly ProxySettings _settings;

    public ProxyStatistics Statistics { get; } = new();
    public int Connections => _clients.Count;
    public IReadOnlyList<ClientInfo> Clients => _clients.Values.ToList().AsReadOnly();
    public IReadOnlyList<BannedClient> Blacklist => _blackList.Values.ToList().AsReadOnly();

    public Proxy(int port, ProxySettings settings)
    {
        _proxyListener = new TcpListener(IPAddress.Any, port);

        Statistics.Port = port;
        Statistics.StartTime = DateTime.UtcNow;

        _settings = settings;
    }

    public void Ban(ClientInfo client, int minutes)
    {
        var ip = client.EndPoint.Address.ToString();
        client.KillAll();
        
        _blackList[ip] = new BannedClient(ip, DateTime.UtcNow.AddMinutes(minutes));
        var removed = _clients.TryRemove(client.Login, out _);
    
        if (_settings.UseDebug)
            Logger.Log($"[DEBUG] User {client.Login} removed from active list: {removed}", ConsoleColor.Gray);
    }

    public void Start()
    {
        _proxyListener.Start();
        
        _ = HandleProxyLoop();

        if (_settings.UseDebug)
            Logger.Log($"PROXY SERVER START LISTENING", ConsoleColor.Green);
    }

    private async Task HandleProxyLoop()
    {
        while (true)
        {
            var client = await _proxyListener.AcceptTcpClientAsync();
            //client.ReceiveTimeout = _settings.TimeoutMs;
            
            var ip = (IPEndPoint)client.Client.RemoteEndPoint;
            var ipStr = ip?.Address.ToString();
            
            if (string.IsNullOrEmpty(ipStr))
            {
                client.Close();
                continue;
            }

            if (_blackList.TryGetValue(ipStr, out var banned))
            {
                if (DateTime.UtcNow <  banned.Until)
                {
                    if (_settings.UseDebug)
                        Logger.Log($"BANNED IP {ipStr} DISCONNECTED. Ban until: {banned.Until}", ConsoleColor.Red);
                    
                    client.Close();
                    continue;
                }
                else
                {
                    if (_blackList.TryRemove(ipStr, out _))
                    {
                        if (_settings.UseDebug)
                            Logger.Log($"[UNBAN] IP {ipStr} has been automatically unbanned", ConsoleColor.Green);
                    }
                }
            }

            _ = HandleProxy(client);
        }
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

            if (_settings.UseDebug) 
                Logger.Log($"[AUTH] SENT 407 AUTH REQUEST TO CLIENT", ConsoleColor.Cyan);

            client.Close();
            clientStream.Close();
            return;
        }

        var allowed = _settings.AllowedConnections.FirstOrDefault(u => u.Login == login && u.Password == password);
        if (allowed == null)
        {
            client.Close();
            clientStream.Close();
            
            if (_settings.UseDebug) 
                Logger.Log($"[AUTH] UNKNOWN CONNECTION: {login}:{password}", ConsoleColor.Red);
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

            if (_settings.UseDebug) 
                Logger.Log($"[LIMIT] User '{login}' exceeded limit ({allowed.MaxConnections})", ConsoleColor.Magenta);
            return;
        }

        var host = ctx.Value.Host;
        var port = int.Parse(ctx.Value.Port);
        var method = ctx.Value.Method;

        var cts = new CancellationTokenSource();
        ProxySession? session = null;
        Task? clientToServer = null;
        Task? serverToClient = null;
        try
        {
            if (_settings.UseDebug) 
                Logger.Log($"[CONN] CONNECTING TO: {host}:{port}, METHOD={method.ToString()}");

            var server = new TcpClient();
            await server.ConnectAsync(host, port, cts.Token);
            var serverStream = server.GetStream();

            if (method == ProxyMethod.CONNECT)
            {
                var response = Response.GetResponseBytes(Response.CONN_ESTABLISHED);
                await clientStream.WriteAsync(response, cts.Token);
            }
            else
            {
                await serverStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
            }

            session = new ProxySession(host, client, server, clientStream, serverStream, cts);
            clientInfo.Sessions.TryAdd(session, 0);

            clientToServer = Relay(clientInfo, clientStream, serverStream, Direction.CLIENT_TO_HOST, cts.Token);
            serverToClient = Relay(clientInfo, serverStream, clientStream, Direction.HOST_TO_CLIENT, cts.Token);

            await Task.WhenAny(clientToServer, serverToClient);
        }
        catch (Exception ex) 
        {
            if (_settings.UseDebug) Logger.Log($"[PROX ERROR]: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            await cts.CancelAsync();
            
            var tasks = new List<Task>();
            if (clientToServer != null) tasks.Add(clientToServer);
            if (serverToClient != null) tasks.Add(serverToClient);
    
            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
                
            if (session != null)
            {
                session.Close();
                clientInfo?.Sessions.TryRemove(session, out _);
            }

            var remaining = clientInfo.ActiveConnections;
            if (remaining <= 0)
            {
                _clients.TryRemove(login, out _);
                
                if (_settings.UseDebug) 
                    Logger.Log($"[FIN] CLIENT {clientInfo.Login} WAS DISCONNECTED");
            }
            else
            {
                if (_settings.UseDebug) 
                    Logger.Log($"[FIN] CLIENT {clientInfo.Login} HOST DISCONNECTED {host}:{port}");
            }
        }
    }

    private async Task Relay(ClientInfo client, NetworkStream input, NetworkStream output, Direction direction,
        CancellationToken ct)
    {
        var relayBuffer = new byte[_settings.BufferSize];

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bytes = await input.ReadAsync(relayBuffer, 0, relayBuffer.Length, ct);
                if (bytes == 0) break;

                await output.WriteAsync(relayBuffer, 0, bytes, ct);

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
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
        {
            if (_settings.UseDebug)
            {
                Logger.Log($"[RELAY ERROR] {direction} for client {client.Login}: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}

public enum Direction
{
    CLIENT_TO_HOST,
    HOST_TO_CLIENT,
}