using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ProxyServer.Tcp
{
    public class TcpWriteService
    {
        private readonly ConcurrentQueue<byte[]> _writeBuffers = new ConcurrentQueue<byte[]>();
        private readonly NetworkStream _stream;
        private readonly CancellationToken _ct;
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public bool Running { get; private set; }
        public Action? OnDisconnected { get; set; }

        public TcpWriteService(NetworkStream stream, CancellationToken ct = default)
        {
            _stream = stream;
            _ct = ct;
        }

        public void Send(byte[] data)
        {
            _writeBuffers.Enqueue(data);
            _signal.Release();
        }

        public void Stop()
        {
            Running = false;
        }

        public void Start()
        {
            Running = true;
            _ = WriteLoopAsync();
        }

        private async Task WriteLoopAsync()
        {
            try
            {
                while (Running)
                {
                    await _signal.WaitAsync(_ct);

                    while (_writeBuffers.TryDequeue(out var buffer))
                    {
                        await _stream.WriteAsync(buffer, 0, buffer.Length, _ct);
                    }

                    await _stream.FlushAsync(_ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Write error: {ex.Message}");
            }
            finally
            {
                Stop();
                OnDisconnected?.Invoke();
            }
        }
    }
}