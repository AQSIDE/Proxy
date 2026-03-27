using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ProxyServer.Buffer;
using ProxyServer.Packets;

namespace ProxyServer;

public class Proxy
{
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
    private readonly ConcurrentDictionary<string, BannedClient> _blackList = new();

    private readonly TcpListener _proxyListener;
    private readonly bool _debugMode;

    public ProxySettingsLoader SettingsLoader { get; }
    public ProxySettings Settings { get; private set; }
    public ProxyStatistics Statistics { get; } = new();
    public int Connections => _clients.Count;
    public IReadOnlyList<ClientInfo> Clients => _clients.Values.ToList().AsReadOnly();
    public IReadOnlyList<BannedClient> Blacklist => _blackList.Values.ToList().AsReadOnly();

    public Proxy(ProxySettingsLoader loader, int port, bool debugMode)
    {
        _debugMode = debugMode;
        _proxyListener = new TcpListener(IPAddress.Any, port);
        //_proxyListener.Server.DualMode = true;

        SettingsLoader = loader;
        Statistics.Port = port;
        Statistics.StartTime = DateTime.UtcNow;
    }

    public void Ban(ClientInfo client, int minutes)
    {
        var ip = client.EndPoint.Address.ToString();
        client.KillAll();

        _blackList[ip] = new BannedClient(ip, DateTime.UtcNow.AddMinutes(minutes));
        var removed = _clients.TryRemove(client.Login, out _);

        if (_debugMode)
            Logger.Log($"[DEBUG] User {client.Login} removed from active list: {removed}", ConsoleColor.Yellow);
    }

    public void Kill(ClientInfo client)
    {
        client.KillAll();

        var removed = _clients.TryRemove(client.Login, out _);

        if (_debugMode)
            Logger.Log($"[DEBUG] User {client.Login} was killed: {removed}", ConsoleColor.Red);
    }

    public void LoadSettings(ProxySettings settings)
    {
        Settings = settings;
        
        if (!_clients.IsEmpty)
        {
            foreach (var client in _clients.Values)
            {
                client.KillAll();
            }
        
            _clients.Clear();
        }
    }

    public void Start()
    {
        _proxyListener.Start();

        _ = HandleProxyLoop();

        if (_debugMode)
            Logger.Log($"PROXY SERVER START LISTENING", ConsoleColor.Green);
    }

    private async Task HandleProxyLoop()
    {
        while (true)
        {
            var client = await _proxyListener.AcceptTcpClientAsync();
            //client.ReceiveTimeout = _settings.TimeoutMs;

            var ip = client.Client.RemoteEndPoint as IPEndPoint;
            var ipStr = ip?.Address.ToString();

            if (string.IsNullOrEmpty(ipStr))
            {
                client.Close();
                continue;
            }

            if (_blackList.TryGetValue(ipStr, out var banned))
            {
                if (DateTime.UtcNow < banned.Until)
                {
                    if (_debugMode)
                        Logger.Log($"BANNED IP {ipStr} DISCONNECTED. Ban until: {banned.Until}", ConsoleColor.Red);

                    client.Close();
                    continue;
                }
                else
                {
                    if (_blackList.TryRemove(ipStr, out _))
                    {
                        if (_debugMode)
                            Logger.Log($"[UNBAN] IP {ipStr} has been automatically unbanned", ConsoleColor.Green);
                    }
                }
            }

            _ = HandleProxy(client, ip);
        }
    }

    private async Task HandleProxy(TcpClient client, IPEndPoint ip)
    {
        ClientInfo? clientInfo = null;
        var session = new ProxySession(new CancellationTokenSource());

        try
        {
            session.Client = client;
            
            var clientStream = client.GetStream();
            session.ClientStream = clientStream;
            
            var buffer = new byte[Settings.HandshakeBufferSize];
            var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length, session.Cts.Token);
            if (bytesRead == 0)
            {
                if (_debugMode) Logger.Log($"[CLOSE] Client {ip} disconnected immediately.", ConsoleColor.Red);
                return;
            }

            var protocol = Protocol.GetProtocol(buffer);
            if (protocol == ProtocolType.UNKNOWN)
            {
                if (_debugMode) Logger.Log($"[WARN] Unknown protocol from {ip}. Dropping.", ConsoleColor.Red);
                return;
            }

            var isAuthOk = false;
            var totalRead = await Protocol.ReadFullPacket(clientStream, buffer, bytesRead, protocol);
            
            if (_debugMode)
                Logger.Log($"[DETECT] Protocol: {protocol} | Bytes: {totalRead} | Initial Bytes: {bytesRead}", ConsoleColor.Green);
            
            if (protocol == ProtocolType.HTTP)
            {
                isAuthOk = await HandleHttp(buffer, session, totalRead);
            }
            else if (protocol == ProtocolType.SOCKS5)
            {
                isAuthOk = await HandleSOCKS5(session);
            }
            
            if (!isAuthOk)
            {
                if (_debugMode) Logger.Log($"[ERROR] Parser failed to get context for {protocol}", ConsoleColor.Red);
                return;
            }

            clientInfo = _clients.GetOrAdd(session.Login, (l) => new ClientInfo(l, ip));
            clientInfo.Sessions.TryAdd(session, 0);
            
            if (clientInfo.ActiveConnections > session.MaxConnections)
            {
                if (_debugMode)
                    Logger.Log($"[LIMIT] User '{session.Login}' exceeded limit ({session.MaxConnections})",
                        ConsoleColor.Magenta);
                return;
            }
            
            clientInfo.LastActive = DateTime.UtcNow;

            if (_debugMode)
                Logger.Log($"[CONN] SUCCESS CONNECTING: HOST={session.Host}:{session.Port}, USER={session.Login}, PROTOCOL={protocol.ToString()}", ConsoleColor.Green);

            var clientToServer = Relay(clientInfo, clientStream, session.ServerStream!, Direction.CLIENT_TO_HOST, session.Cts);
            var serverToClient = Relay(clientInfo, session.ServerStream!, clientStream, Direction.HOST_TO_CLIENT, session.Cts);

            await Task.WhenAny(clientToServer, serverToClient);
        }
        catch (Exception ex)
        {
            if (_debugMode) Logger.Log($"[PROX ERROR]: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            session.Close();
            
            if (clientInfo != null)
            {
                clientInfo.Sessions.TryRemove(session, out _);
                
                if (clientInfo.Sessions.IsEmpty)
                {
                    _clients.TryRemove(clientInfo.Login, out _);

                    if (_debugMode)
                        Logger.Log($"[FIN] CLIENT {clientInfo.Login} WAS DISCONNECTED");
                }
                else
                {
                    if (_debugMode)
                        Logger.Log($"[FIN] CLIENT {clientInfo.Login} HOST DISCONNECTED {session.Host}:{session.Port}");
                }
            }
        }
    }

    private async Task Relay(ClientInfo client, NetworkStream input, NetworkStream output, Direction direction,
        CancellationTokenSource cts)
    {
        var relayBuffer = new byte[Settings.RelayBufferSize];
        var idleTimer = TimeSpan.FromSeconds(Settings.TimeoutSec);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                cts.CancelAfter(idleTimer);
                
                var bytes = await input.ReadAsync(relayBuffer, 0, relayBuffer.Length, cts.Token);
                if (bytes == 0) break;
                
                cts.CancelAfter(idleTimer);

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
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
        {
            if (_debugMode)
            {
                Logger.Log($"[RELAY ERROR] {direction} for client {client.Login}: {ex.Message}", ConsoleColor.Red);
            }
        }
    }

    private async Task<bool> HandleSOCKS5(ProxySession session)
    {
        await session.ClientStream!.WriteAsync(Response.AUTH_SOCKS5, 0, Response.AUTH_SOCKS5.Length, session.Cts.Token);
                
        var authBuffer = new byte[512];
        var authRead = await session.ClientStream.ReadAsync(authBuffer, 0, authBuffer.Length, session.Cts.Token);
        if (authRead < 3) return false;

        var readBuffer = new ReadBuffer(authBuffer);
        readBuffer.EndianType = EndianType.Big;
        
        var authPacket = new Socks5AuthPacket();
        authPacket.Read(readBuffer);

        var login = authPacket.Login;
        var password = authPacket.Password;
        session.Login = login;
        
        var allowed = Settings.AllowedConnections.FirstOrDefault(u => u.Login == login && u.Password == password);
        if (allowed == null)
        {
            await SendNotAuth(session.ClientStream!, login, ProtocolType.SOCKS5, session.Cts.Token);
            return false;
        }
        
        session.MaxConnections = allowed.MaxConnections;
        await session.ClientStream.WriteAsync(new byte[] { 0x01, 0x00 }, 0, 2, session.Cts.Token);
        
        var requestBuffer = new byte[512];
        var requestRead = await session.ClientStream.ReadAsync(requestBuffer, 0, requestBuffer.Length, session.Cts.Token);
        if (requestRead < 7) return false;
        
        readBuffer = new ReadBuffer(requestBuffer);
        readBuffer.EndianType = EndianType.Big;
        
        var requestPacket = new Socks5RequestPacket();
        requestPacket.Read(readBuffer);
        
        var host = requestPacket.Host;
        var port = requestPacket.Port;
        session.Host = host;
        session.Port = port;
        
        try 
        {
            var server = new TcpClient();
            session.Server = server;
            
            await server.ConnectAsync(host, port, session.Cts.Token);
            session.ServerStream = server.GetStream();

            await session.ClientStream.WriteAsync(Response.SUCCESS_SOCKS5, 0, Response.SUCCESS_SOCKS5.Length, session.Cts.Token);
            
            if (_debugMode)
                Logger.Log($"[SOCKS5] Client requests CONNECT to {host}:{port}");
        }
        catch (Exception ex)
        {
            if (_debugMode) Logger.Log($"[SOCKS5] Target connect failed: {ex.Message}", ConsoleColor.Red);
            
            await session.ClientStream.WriteAsync(Response.HOST_UNREACHABLE_SOCKS5, 0, Response.HOST_UNREACHABLE_SOCKS5.Length, session.Cts.Token);
            return false;
        }
        
        session.ProtocolType = ProtocolType.SOCKS5;
        return true;
    }

    private async Task<bool> HandleHttp(byte[] buffer, ProxySession session, int totalRead)
    {
        var context = Parser.GetContext(buffer, totalRead, ProtocolType.HTTP);
        if (context == null) return false;
        
        var login = context.Value.Login;
        var password = context.Value.Password;
        session.Login = login;
    
        var allowed = Settings.AllowedConnections.FirstOrDefault(u => u.Login == login && u.Password == password);
        if (allowed == null)
        {
            await SendNotAuth(session.ClientStream!, login, ProtocolType.HTTP, session.Cts.Token);
            return false;
        }
        
        session.MaxConnections = allowed.MaxConnections;
        
        var host = context.Value.Host;
        var port = int.Parse(context.Value.Port);
        session.Host = host;
        session.Port = port;
        
        var server = new TcpClient();
        session.Server = server;
            
        await server.ConnectAsync(host, port, session.Cts.Token);
        var serverStream = server.GetStream();
        session.ServerStream = serverStream;
        
        if (context.Value.Method == ProxyMethod.CONNECT)
        {
            var response = Response.GetResponseBytes(Response.CONN_ESTABLISHED);
            await session.ClientStream!.WriteAsync(response, session.Cts.Token);
        }
        else
        {
            await session.ServerStream!.WriteAsync(buffer, 0, totalRead, session.Cts.Token);
        }
        
        session.ProtocolType = ProtocolType.HTTP;
        return true;
    }

    private async Task SendNotAuth(NetworkStream clientS, string login, ProtocolType protocol, CancellationToken ct)
    {
        byte[] errorResponse;
        if (protocol == ProtocolType.HTTP)
        {
            errorResponse = Response.GetResponseBytes(Response.AUTH_REQUIRED_HTTP);
        }
        else
        {
            errorResponse = Response.FAILURE_SOCKS5;
        }

        await clientS.WriteAsync(errorResponse, 0, errorResponse.Length, ct);

        if (_debugMode)
            Logger.Log($"[AUTH] FAILED AUTH for '{login}' (Protocol: {protocol})", ConsoleColor.Red);
    }
}

public enum Direction
{
    CLIENT_TO_HOST,
    HOST_TO_CLIENT,
}