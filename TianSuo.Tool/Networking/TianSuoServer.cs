using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace TianSuo.Tool.Networking
{
    public sealed class TianSuoServer
    {
        private const string MagicGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private readonly ListenerOptions _options;
        private readonly ConcurrentDictionary<long, Session> _sessions = new ConcurrentDictionary<long, Session>();

        private TcpListener _listener;
        private CancellationTokenSource _shutdown;
        private Task _acceptLoop;
        private long _nextSessionId;
        private readonly object _stateLock = new object();

        public event Action<Session> Connected;
        public event Action<Session, string> MessageReceived;
        public event Action<Session> Disconnected;

        public int ConnectedCount => _sessions.Count;

        public TianSuoServer(ListenerOptions options)
        {
            _options = options;
        }

        public void Start()
        {
            lock (_stateLock)
            {
                if (_acceptLoop != null)
                    return;

                _shutdown = new CancellationTokenSource();
                _listener = new TcpListener(System.Net.IPAddress.Parse(_options.Host), _options.Port);
                _listener.Start();
                _acceptLoop = Task.Run(AcceptLoop);
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (_acceptLoop == null)
                    return;

                _shutdown.Cancel();
                _listener.Stop();
                _acceptLoop = null;

                foreach (var session in _sessions.Values)
                {
                    var close = session.CloseAsync(WebSocketCloseStatus.NormalClosure, "server shutting down");
                    close.Wait(TimeSpan.FromSeconds(2));
                }

                _sessions.Clear();
            }
        }

        public void Broadcast(string payload)
        {
            foreach (var session in _sessions.Values)
            {
                if (!session.Authenticated)
                    continue;

                var _ = session.SendAsync(payload);
            }
        }

        private async Task AcceptLoop()
        {
            var token = _shutdown.Token;
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch
                {
                    break;
                }

                var _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();

                try
                {
                    var headers = await ReadHandshakeAsync(stream, _shutdown.Token);

                    if (ReadHeader(headers, "Upgrade") != "websocket")
                    {
                        await WriteHttpErrorAsync(stream, 400, "Bad Request");
                        return;
                    }

                    var clientName = ReadHeader(headers, "x-self-name");
                    if (string.IsNullOrEmpty(clientName) || clientName != _options.ServerName)
                    {
                        await WriteHttpErrorAsync(stream, 401, "Unauthorized: server name mismatch");
                        return;
                    }

                    if (!string.IsNullOrEmpty(_options.AccessToken))
                    {
                        var authorization = ReadHeader(headers, "Authorization");
                        if (authorization != "Bearer " + _options.AccessToken)
                        {
                            await WriteHttpErrorAsync(stream, 401, "Unauthorized: invalid token");
                            return;
                        }
                    }

                    var webSocketKey = ReadHeader(headers, "Sec-WebSocket-Key");
                    var response = BuildUpgradeResponse(webSocketKey);
                    await stream.WriteAsync(response, 0, response.Length);
                    await stream.FlushAsync();

                    using var socket = WebSocket.CreateFromStream(stream, true, null, TimeSpan.FromSeconds(30));
                    var session = new Session(Interlocked.Increment(ref _nextSessionId), socket, client.Client.RemoteEndPoint?.ToString())
                    {
                        ClientName = clientName,
                        Authenticated = true,
                    };

                    _sessions[session.Id] = session;
                    Connected?.Invoke(session);

                    try
                    {
                        await session.RunReceiveLoopAsync(OnSessionMessage, _shutdown.Token);
                    }
                    finally
                    {
                        _sessions.TryRemove(session.Id, out _);
                        Disconnected?.Invoke(session);
                    }
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
                catch (WebSocketException)
                {
                }
            }
        }

        private Task OnSessionMessage(Session session, string payload)
        {
            MessageReceived?.Invoke(session, payload);
            return Task.CompletedTask;
        }

        private static async Task<Dictionary<string, string>> ReadHandshakeAsync(NetworkStream stream, CancellationToken token)
        {
            using var buffer = new MemoryStream();
            var matched = 0;

            while (matched < 4)
            {
                var next = await ReadByteAsync(stream, token);
                if (next < 0)
                    break;

                buffer.WriteByte((byte)next);

                var expected = matched % 2 == 0 ? (byte)'\r' : (byte)'\n';
                if (next == expected)
                    matched++;
                else
                    matched = next == '\r' ? 1 : 0;
            }

            return ParseHeaders(Encoding.ASCII.GetString(buffer.ToArray()));
        }

        private static async Task<int> ReadByteAsync(NetworkStream stream, CancellationToken token)
        {
            var buffer = new byte[1];
            var read = await stream.ReadAsync(buffer.AsMemory(), token);
            return read == 0 ? -1 : buffer[0];
        }

        private static Dictionary<string, string> ParseHeaders(string rawRequest)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in rawRequest.Split("\r\n"))
            {
                var separator = line.IndexOf(':');
                if (separator <= 0)
                    continue;

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                headers[key] = value;
            }

            return headers;
        }

        private static string ReadHeader(Dictionary<string, string> headers, string name)
        {
            return headers.TryGetValue(name, out var value) ? value : null;
        }

        private static byte[] BuildUpgradeResponse(string webSocketKey)
        {
            var accept = ComputeAccept(webSocketKey);
            var response = "HTTP/1.1 101 Switching Protocols\r\n"
                           + "Upgrade: websocket\r\n"
                           + "Connection: Upgrade\r\n"
                           + "Sec-WebSocket-Accept: " + accept + "\r\n"
                           + "\r\n";
            return Encoding.ASCII.GetBytes(response);
        }

        private static string ComputeAccept(string webSocketKey)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(webSocketKey + MagicGuid));
            return Convert.ToBase64String(hash);
        }

        private static async Task WriteHttpErrorAsync(NetworkStream stream, int code, string reason)
        {
            var body = Encoding.UTF8.GetBytes(reason);
            var response = "HTTP/1.1 " + code + " " + reason + "\r\n"
                           + "Connection: close\r\n"
                           + "Content-Length: " + body.Length + "\r\n"
                           + "\r\n";
            var responseBytes = Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }
    }
}
