using System.Net.Sockets;
using ProxyServer.Buffer;

namespace ProxyServer.Tcp
{
    public class TcpReadService
    {
        private readonly NetworkStream _stream;
        private readonly CancellationToken _ct;
    
        public bool Running { get; private set; }
        public Action<byte[]>? PacketReceived { get; set; }
        public Action? OnDisconnected { get; set; }

        public TcpReadService(NetworkStream stream, CancellationToken ct = default)
        {
            _stream = stream;
            _ct = ct;
        }

        public void Stop()
        {
            Running = false;
        }

        public void Start()
        {
            Running = true;
            _ = ReadLoopAsync();
        }

        private async Task ReadLoopAsync()
        {
            try
            {
                while (Running)
                {
                    var packet = await ReadPacketAsync();
                    if (packet == null) break;
                
                    PacketReceived?.Invoke(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сети: {ex.Message}");
            }
            finally
            {
                Stop();
                OnDisconnected?.Invoke();
            }
        }
    
        private async Task<byte[]?> ReadPacketAsync()
        {
            var lengthBuffer = new byte[4];
            if (!await FillBufferAsync(lengthBuffer)) return null;

            var headerReader = new ReadBuffer(lengthBuffer);
            var length = headerReader.ReadInt();
        
            if (length <= 0 || length > 1024 * 1024)
                throw new Exception("Некорректный размер пакета");

            var packetBuffer = new byte[length];
        
            if (!await FillBufferAsync(packetBuffer)) return null;
        
            return packetBuffer;
        }
    
        private async Task<bool> FillBufferAsync(byte[] buffer)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var n = await _stream.ReadAsync(buffer, read, buffer.Length - read, _ct);
                if (n == 0) return false;
                read += n;
            }
            return true;
        }
    }
}

