using System.Net.WebSockets;
using System.Text;

namespace TianSuo.Tool.Networking
{
    public sealed class Session
    {
        private readonly WebSocket _socket;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private volatile bool _closed;

        public long Id { get; }
        public string ClientName { get; internal set; }
        public bool Authenticated { get; internal set; }
        public string RemoteEndPoint { get; }

        internal Session(long id, WebSocket socket, string remoteEndPoint)
        {
            Id = id;
            _socket = socket;
            RemoteEndPoint = remoteEndPoint;
        }

        public async Task SendAsync(string payload)
        {
            if (_closed)
                return;

            var bytes = Encoding.UTF8.GetBytes(payload);
            try
            {
                await _sendLock.WaitAsync();
                try
                {
                    await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch
            {
                _closed = true;
            }
        }

        internal async Task RunReceiveLoopAsync(Func<Session, string, Task> onMessage, CancellationToken token)
        {
            var buffer = new byte[8192];
            var message = new MemoryStream();

            while (!token.IsCancellationRequested)
            {
                message.SetLength(0);
                var endOfMessage = false;

                while (!endOfMessage)
                {
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        try
                        {
                            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                        }
                        catch
                        {
                        }

                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                        message.Write(buffer, 0, result.Count);

                    endOfMessage = result.EndOfMessage;
                }

                if (message.Length > 0)
                {
                    var text = Encoding.UTF8.GetString(message.ToArray());
                    await onMessage(this, text);
                }
            }
        }

        internal async Task CloseAsync(WebSocketCloseStatus status, string reason)
        {
            _closed = true;
            try
            {
                await _socket.CloseAsync(status, reason, CancellationToken.None);
            }
            catch
            {
            }
        }
    }
}
