using PHS.Networking.Enums;
using PHS.Networking.Events.Args;
using PHS.Networking.Server.Enums;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebsocketsSimple.Core;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public abstract class WebsocketHandlerBase<T, U, V, W, Z> :
        HandlerServerBase<T, U, V, W, Z>
        where T : WSConnectionServerBaseEventArgs<Z>
        where U : WSMessageServerBaseEventArgs<Z>
        where V : WSErrorServerBaseEventArgs<Z>
        where W : ParamsWSServer
        where Z : ConnectionWSServer
    {
        protected TcpListener _server;

        protected byte[] _certificate;
        protected string _certificatePassword;

        public WebsocketHandlerBase(W parameters) : base(parameters)
        {
        }
        public WebsocketHandlerBase(W parameters, byte[] certificate, string certificatePassword) : base(parameters)
        {
            _certificate = certificate;
            _certificatePassword = certificatePassword;
        }

        public override void Start(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_server != null)
                {
                    Stop(cancellationToken);
                }

                _isRunning = true;

                _server = new TcpListener(IPAddress.Any, _parameters.Port);
                _server.Server.ReceiveTimeout = 60000;
                _server.Start();

                FireEvent(this, new ServerEventArgs
                {
                    ServerEventType = ServerEventType.Start
                });

                if (_certificate == null)
                {
                    _ = Task.Run(async () => { await ListenForConnectionsAsync(cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _ = Task.Run(async () => { await ListenForConnectionsSSLAsync(cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
                }
                return;
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                {
                    Exception = ex,
                    Message = ex.Message,
                    CancellationToken = cancellationToken
                }));
            }
        }

        public override void Stop(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_server != null)
                {
                    _server.Stop();
                    _server = null;
                }

                FireEvent(this, new ServerEventArgs
                {
                    ServerEventType = ServerEventType.Stop,
                    CancellationToken = cancellationToken
                });
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                {
                    Exception = ex,
                    Message = ex.Message,
                    CancellationToken = cancellationToken
                }));
            }

            _isRunning = false;
        }

        protected virtual async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                TcpClient client = null;

                try
                {
                    client = await _server.AcceptTcpClientAsync().ConfigureAwait(false);

                    var connection = CreateConnection(new ConnectionWSServer
                    {
                        TcpClient = client
                    });

                    _ = Task.Run(async () => { await ReceiveAsync(connection, cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        CancellationToken = cancellationToken
                    }));

                    try
                    {
                        client?.GetStream().Close();
                    }
                    catch { }

                    try
                    {
                        client?.Dispose();
                    }
                    catch { }
                }
            }
        }
        protected virtual async Task ListenForConnectionsSSLAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                TcpClient client = null;

                try
                {
                    client = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
                    var sslStream = new SslStream(client.GetStream());
                    await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = new X509Certificate2(_certificate, _certificatePassword)
                    }, cancellationToken).ConfigureAwait(false);

                    if (sslStream.IsAuthenticated && sslStream.IsEncrypted)
                    {
                        var connection = CreateConnection(new ConnectionWSServer
                        {
                            TcpClient = client,
                            SslStream = sslStream,
                            ReadBuffer = new byte[4096]
                        });

                        _ = Task.Run(async () => { await ReceiveSSLAsync(connection, cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var certStatus = $"IsAuthenticated = {sslStream.IsAuthenticated} && IsEncrypted == {sslStream.IsEncrypted}";
                        FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                        {
                            Exception = new Exception(certStatus),
                            Message = certStatus,
                            CancellationToken = cancellationToken
                        }));

                        client?.GetStream().Close();
                        client?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        CancellationToken = cancellationToken
                    }));

                    try
                    {
                        client?.GetStream().Close();
                    }
                    catch { }

                    try
                    {
                        client?.Dispose();
                    }
                    catch { }
                }
            }
        }

        protected virtual async Task ReceiveAsync(Z connection, CancellationToken cancellationToken)
        {
            while (connection.TcpClient.Connected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (connection.Websocket == null)
                    {
                        if (connection.TcpClient.Available < 3)
                        {
                            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                            // match against "get"
                            continue;
                        };

                        var bytes = new byte[connection.TcpClient.Available];
                        var buffer = new ArraySegment<byte>(bytes);
                        await connection.TcpClient.Client.ReceiveAsync(bytes, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                        var data = Encoding.UTF8.GetString(bytes);

                        if (Regex.IsMatch(data, "^GET", RegexOptions.IgnoreCase))
                        {
                            var requestSubprotocols = Regex.Match(data, $"{HttpKnownHeaderNames.SecWebSocketProtocol}: (.*)").Groups[1].Value.Trim().Split(",");

                            var headers = new Dictionary<string, string>();
                            foreach (var item in data.Split("\r\n").Where(x => !string.IsNullOrWhiteSpace(x)))
                            {
                                var split = item.Split(":");

                                if (split.Length == 2)
                                {
                                    headers.Add(split[0].Trim(), split[1].Trim());
                                }
                            }

                            if (await CanUpgradeConnection(data, requestSubprotocols, headers, connection, cancellationToken).ConfigureAwait(false))
                            {
                                await UpgradeConnectionAsync(data, requestSubprotocols, headers, connection, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        WebSocketReceiveResult result = null;
                        {
                            do
                            {
                                try
                                {
                                    if (connection.TcpClient.Available > 0)
                                    {
                                        var buffer = WebSocket.CreateServerBuffer(connection.TcpClient.Available);
                                        result = await connection.Websocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                        await connection.MemoryStream.WriteAsync(buffer.Array.AsMemory(buffer.Offset, result.Count), cancellationToken).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                catch { }
                            }
                            while (connection != null && connection.TcpClient.Connected && connection.Websocket.State == WebSocketState.Open && result != null && !result.EndOfMessage);

                            if (connection != null && result != null && result.EndOfMessage)
                            {
                                var bytes = connection.MemoryStream.ToArray();
                                connection.MemoryStream.SetLength(0);

                                switch (result.MessageType)
                                {
                                    case WebSocketMessageType.Text:
                                        FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                                        {
                                            MessageEventType = MessageEventType.Receive,
                                            Message = Encoding.UTF8.GetString(bytes),
                                            Bytes = bytes,
                                            Connection = connection,
                                            CancellationToken = cancellationToken
                                        }));
                                        break;
                                    case WebSocketMessageType.Binary:
                                        FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                                        {
                                            MessageEventType = MessageEventType.Receive,
                                            Bytes = bytes,
                                            Connection = connection,
                                            CancellationToken = cancellationToken
                                        }));
                                        break;
                                    case WebSocketMessageType.Close:
                                        await DisconnectConnectionAsync(connection, WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken: cancellationToken).ConfigureAwait(false);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        Connection = connection,
                        CancellationToken = cancellationToken
                    }));
                }
            }

            FireEvent(this, CreateConnectionEventArgs(new WSConnectionServerBaseEventArgs<Z>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Disconnect,
                CancellationToken = cancellationToken
            }));
        }

        protected virtual async Task ReceiveSSLAsync(Z connection, CancellationToken cancellationToken)
        {
            while (connection.TcpClient.Connected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (connection.Websocket == null)
                    {
                        if (connection.TcpClient.Available > 0)
                        {
                            var bytesRead = 0;
                            if ((bytesRead = connection.SslStream.Read(connection.ReadBuffer, 0, connection.ReadBuffer.Length)) > 0)
                            {
                                await connection.MemoryStream.WriteAsync(connection.ReadBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                                connection.ReadBuffer = new byte[4096];
                            }
                        }
                        else
                        {
                            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        var data = Encoding.UTF8.GetString(connection.MemoryStream.ToArray());
                        connection.MemoryStream.SetLength(0);

                        if (Regex.IsMatch(data, "^GET", RegexOptions.IgnoreCase))
                        {
                            var requestSubprotocols = Regex.Match(data, $"{HttpKnownHeaderNames.SecWebSocketProtocol}: (.*)").Groups[1].Value.Trim().Split(",");

                            var headers = new Dictionary<string, string>();
                            foreach (var item in data.Split("\r\n").Where(x => !string.IsNullOrWhiteSpace(x)))
                            {
                                var split = item.Split(":");

                                if (split.Length == 2)
                                {
                                    headers.Add(split[0].Trim(), split[1].Trim());
                                }
                            }

                            if (await CanUpgradeConnection(data, requestSubprotocols, headers, connection, cancellationToken).ConfigureAwait(false))
                            {
                                await UpgradeConnectionAsync(data, requestSubprotocols, headers, connection, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        WebSocketReceiveResult result = null;
                        {
                            do
                            {
                                try
                                {
                                    if (connection.TcpClient.Available > 0)
                                    {
                                        var buffer = WebSocket.CreateServerBuffer(connection.TcpClient.Available);
                                        result = await connection.Websocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                        await connection.MemoryStream.WriteAsync(buffer.Array.AsMemory(buffer.Offset, result.Count), cancellationToken).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                catch { }
                            }
                            while (connection != null && connection.TcpClient.Connected && connection.Websocket.State == WebSocketState.Open && result != null && !result.EndOfMessage);

                            if (connection != null && result != null && result.EndOfMessage)
                            {
                                var bytes = connection.MemoryStream.ToArray();
                                connection.MemoryStream.SetLength(0);

                                switch (result.MessageType)
                                {
                                    case WebSocketMessageType.Text:
                                        FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                                        {
                                            MessageEventType = MessageEventType.Receive,
                                            Message = Encoding.UTF8.GetString(bytes),
                                            Bytes = bytes,
                                            Connection = connection,
                                            CancellationToken = cancellationToken
                                        }));
                                        break;
                                    case WebSocketMessageType.Binary:
                                        FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                                        {
                                            MessageEventType = MessageEventType.Receive,
                                            Bytes = bytes,
                                            Connection = connection,
                                            CancellationToken = cancellationToken
                                        }));
                                        break;
                                    case WebSocketMessageType.Close:
                                        await DisconnectConnectionAsync(connection, WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken: cancellationToken).ConfigureAwait(false);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                        Connection = connection,
                        CancellationToken = cancellationToken
                    }));
                }
            }

            FireEvent(this, CreateConnectionEventArgs(new WSConnectionServerBaseEventArgs<Z>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Disconnect,
                CancellationToken = cancellationToken
            }));
        }

        public override async Task<bool> SendAsync(string message, Z connection, CancellationToken cancellationToken)
        {
            try
            {
                if (connection.TcpClient != null &&
                    connection.TcpClient.Connected &&
                    connection.Websocket != null &&
                    connection.Websocket.State == WebSocketState.Open && 
                    !cancellationToken.IsCancellationRequested &&
                    _isRunning &&
                    !string.IsNullOrWhiteSpace(message))
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: bytes,
                        offset: 0,
                        count: message.Length),
                        messageType: WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                    {
                        MessageEventType = MessageEventType.Sent,
                        Message = message,
                        Bytes = bytes,
                        Connection = connection,
                        CancellationToken = cancellationToken
                    }));
                }

                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                {
                    Exception = ex,
                    Message = ex.Message,
                    Connection = connection,
                    CancellationToken = cancellationToken
                }));
            }

            await DisconnectConnectionAsync(connection, cancellationToken).ConfigureAwait(false);

            return false;
        }
        public override async Task<bool> SendAsync(byte[] message, Z connection, CancellationToken cancellationToken)
        {
            try
            {
                if (connection.TcpClient != null &&
                    connection.TcpClient.Connected &&
                    connection.Websocket != null &&
                    connection.Websocket.State == WebSocketState.Open &&
                    !cancellationToken.IsCancellationRequested &&
                    _isRunning &&
                    message.Where(x => x != 0).Any())
                {
                    await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: message,
                        offset: 0,
                        count: message.Length),
                        messageType: WebSocketMessageType.Binary,
                        endOfMessage: true,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                    {
                        MessageEventType = MessageEventType.Sent,
                        Bytes = message,
                        Connection = connection,
                        CancellationToken = cancellationToken
                    }));
                }

                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                    CancellationToken = cancellationToken
                }));
            }

            await DisconnectConnectionAsync(connection, cancellationToken).ConfigureAwait(false);

            return false;
        }

        public override async Task<bool> DisconnectConnectionAsync(Z connection, CancellationToken cancellationToken, string disconnectMessage = "")
        {
            return await DisconnectConnectionAsync(connection, WebSocketCloseStatus.NormalClosure, "Disconnect", disconnectMessage, cancellationToken).ConfigureAwait(false);
        }
        public virtual async Task<bool> DisconnectConnectionAsync(Z connection,
             WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
             string statusDescription = "Disconnect",
             string disconnectMessage = "",
             CancellationToken cancellationToken = default)
        {
            if (connection.Websocket != null && connection.TcpClient != null && connection.TcpClient.Connected && !connection.Disposed)
            {
                connection.Disposed = true;

                try
                {
                    await connection.Websocket.CloseAsync(webSocketCloseStatus, statusDescription, cancellationToken).ConfigureAwait(false);

                    connection?.Dispose();

                    return true;
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message,
                        CancellationToken = cancellationToken
                    }));
                }
            }

            return false;
        }

        protected virtual async Task<bool> CanUpgradeConnection(string message, string[] requestedSubprotocols, Dictionary<string, string> requestHeaders, Z connection, CancellationToken cancellationToken)
        {
            SetPathAndQueryStringForConnection(message, connection);

            if (requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0)
            {
                if (!AreSubprotocolsRequestedValid(requestedSubprotocols))
                {
                    var bytes = Encoding.UTF8.GetBytes("Invalid subprotocols requested");
                    if (connection.SslStream != null)
                    {
                        await connection.SslStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                    }
                    await DisconnectConnectionAsync(connection, WebSocketCloseStatus.ProtocolError, "Invalid Subprotocol", cancellationToken: cancellationToken).ConfigureAwait(false);
                    return false;
                }
            }

            return true;
        }
        protected virtual async Task UpgradeConnectionAsync(string message, string[] requestedSubprotocols, Dictionary<string, string> requestHeaders, Z connection, CancellationToken cancellationToken)
        {
            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            var swk = Regex.Match(message, $"{HttpKnownHeaderNames.SecWebSocketKey}: (.*)").Groups[1].Value.Trim();
            var swka = swk + Statics.WS_SERVER_GUID;
            var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            var swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            var subProtocol = requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0 ? $"{requestedSubprotocols[0]}" : null;

            if (connection.SslStream != null)
            {
                connection.Websocket = WebSocket.CreateFromStream(connection.SslStream, true, subProtocol, TimeSpan.FromSeconds(_parameters.PingIntervalSec));
            }
            else
            {
                connection.Websocket = WebSocket.CreateFromStream(connection.TcpClient.GetStream(), true, subProtocol, TimeSpan.FromSeconds(_parameters.PingIntervalSec));
            }

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                $"{HttpKnownHeaderNames.Connection}: Upgrade\r\n" +
                $"{HttpKnownHeaderNames.Upgrade}: websocket\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketAccept}: {swkaSha1Base64}\r\n" +
                (!string.IsNullOrWhiteSpace(subProtocol) ? $"{HttpKnownHeaderNames.SecWebSocketProtocol}: {subProtocol}\r\n" : "") +
                "\r\n");

            if (connection.SslStream != null)
            {
                await connection.SslStream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(response), SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            
            if (!_parameters.OnlyEmitBytes && !string.IsNullOrWhiteSpace(_parameters.ConnectionSuccessString))
            {
                await SendAsync(_parameters.ConnectionSuccessString, connection, cancellationToken).ConfigureAwait(false);
            }

            FireEvent(this, CreateConnectionEventArgs(new WSConnectionServerBaseEventArgs<Z>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Connected,
                RequestHeaders = requestHeaders,
                CancellationToken = cancellationToken
            }));
        }
        protected virtual void SetPathAndQueryStringForConnection(string message, Z connection)
        {
            // Get Path and QueryStrung and load into connection
            var split = message.Split(" ");

            if (split.Length < 2)
            {
                return;
            }

            var pathAndQueryString = split[1];
            if (pathAndQueryString.IndexOf("?") > 0)
            {
                connection.Channel = pathAndQueryString.StartsWith("/") ? pathAndQueryString.Substring(1, pathAndQueryString.IndexOf("?") - 1) : pathAndQueryString.Substring(0, pathAndQueryString.IndexOf("?") - 1);

                var qsParsedToken = HttpUtility.ParseQueryString(pathAndQueryString.Substring(pathAndQueryString.IndexOf("?") + 1));

                var kvps = new List<KeyValuePair<string, string>>();
                foreach (string item in qsParsedToken)
                {
                    kvps.Add(new KeyValuePair<string, string>(item, qsParsedToken[item]));
                }
                connection.QueryStringParameters = kvps.ToArray();
            }
            else
            {
                connection.Channel = pathAndQueryString;
            }
        }
        protected virtual bool AreSubprotocolsRequestedValid(string[] subprotocols)
        {
            if (_parameters.AvailableSubprotocols == null)
            {
                return false;
            }

            foreach (var item in subprotocols.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!_parameters.AvailableSubprotocols.Any(x => x.Trim().ToLower() == item.Trim().ToLower()))
                {
                    return false;
                }
            }
            return true;
        }

        protected abstract Z CreateConnection(ConnectionWSServer connection);
        protected abstract T CreateConnectionEventArgs(ConnectionEventArgs<Z> args);
        protected abstract U CreateMessageEventArgs(WSMessageServerBaseEventArgs<Z> args);
        protected abstract V CreateErrorEventArgs(ErrorEventArgs<Z> args);

        public TcpListener Server
        {
            get
            {
                return _server;
            }
        }
    }
}