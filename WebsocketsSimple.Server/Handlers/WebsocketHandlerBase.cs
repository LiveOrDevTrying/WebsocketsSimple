using PHS.Networking.Enums;
using PHS.Networking.Events.Args;
using PHS.Networking.Server.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tcp.NET.Core.Models;
using WebsocketsSimple.Core;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public abstract class WebsocketHandlerBase<T, U, V, W, Z> :
        TcpHandlerServerBaseTcp<T, U, V, W, Z>
        where T : WSConnectionServerBaseEventArgs<Z>
        where U : WSMessageServerBaseEventArgs<Z>
        where V : WSErrorServerBaseEventArgs<Z>
        where W : ParamsWSServer
        where Z : ConnectionWSServer
    {
        public WebsocketHandlerBase(W parameters) : base(parameters)
        {
        }
        public WebsocketHandlerBase(W parameters, byte[] certificate, string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }

        public override async Task<bool> SendAsync(string message, Z connection, CancellationToken cancellationToken)
        {
            try
            {
                if (connection.TcpClient.Connected && connection.Websocket != null && connection.Websocket.State == WebSocketState.Open && _isRunning)
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
                    }));

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                {
                    Exception = ex,
                    Message = ex.Message,
                    Connection = connection,
                }));
            }

            return false;
        }
        public override async Task<bool> SendAsync(byte[] message, Z connection, CancellationToken cancellationToken)
        {
            try
            {
                if (connection.TcpClient != null && connection.TcpClient.Connected && connection.Websocket != null && connection.Websocket.State == WebSocketState.Open && _isRunning)
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
                    }));

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                }));
            }

            return false;
        }
        public override async Task<bool> DisconnectConnectionAsync(Z connection, CancellationToken cancellationToken)
        {
            return await DisconnectConnectionAsync(connection, WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken);
        }
        public virtual async Task<bool> DisconnectConnectionAsync(Z connection,
            WebSocketCloseStatus webSocketCloseStatus,
            string statusDescription,
            CancellationToken cancellationToken)
        {
            if (connection.Websocket != null && connection.TcpClient != null && connection.TcpClient.Connected && !connection.Disposed)
            {
                connection.Disposed = true;

                try
                {
                    await connection.Websocket.CloseAsync(webSocketCloseStatus, statusDescription, cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new WSErrorServerBaseEventArgs<Z>
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    }));
                }
            }

            return false;
        }


        protected override async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync().ConfigureAwait(false);

                    var connection = CreateConnection(new ConnectionTcp
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
                    }));
                }
            }
        }
        protected override async Task ListenForConnectionsSSLAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
                    var sslStream = new SslStream(client.GetStream());
                    await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = new X509Certificate2(_certificate, _certificatePassword)
                    }, cancellationToken).ConfigureAwait(false);

                    if (sslStream.IsAuthenticated && sslStream.IsEncrypted)
                    {
                        var connection = CreateConnection(new ConnectionTcp
                        {
                            TcpClient = client
                        });

                        _ = Task.Run(async () => { await ReceiveAsync(connection, cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var certStatus = $"IsAuthenticated = {sslStream.IsAuthenticated} && IsEncrypted == {sslStream.IsEncrypted}";
                        FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                        {
                            Exception = new Exception(certStatus),
                            Message = certStatus
                        }));

                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    FireEvent(this, CreateErrorEventArgs(new ErrorEventArgs<Z>
                    {
                        Exception = ex,
                        Message = ex.Message,
                    }));
                }
            }
        }
        protected override async Task ReceiveAsync(Z connection, CancellationToken cancellationToken)
        {
            while (connection.TcpClient.Connected && !cancellationToken.IsCancellationRequested && !connection.Disposed)
            {
                try
                {
                    if (connection.Websocket == null)
                    {
                        if (connection.TcpClient.Available < 3)
                        {
                            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                            continue;
                        }; // match against "get"

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
                        using (var ms = new MemoryStream())
                        {
                            do
                            {
                                if (connection.Disposed || connection.TcpClient.Available <= 0)
                                {
                                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                                    continue;
                                }

                                var buffer = WebSocket.CreateServerBuffer(connection.TcpClient.Available);
                                result = await connection.Websocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                                await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count).ConfigureAwait(false);
                            }
                            while (result == null || (!connection.Disposed && !result.EndOfMessage && connection != null && connection.TcpClient.Connected && connection.Websocket.State == WebSocketState.Open));
                            
                            switch (result.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    var message = Encoding.UTF8.GetString(ms.ToArray());

                                    if (!string.IsNullOrWhiteSpace(message))
                                    {
                                        FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                                        {
                                            MessageEventType = MessageEventType.Receive,
                                            Message = message,
                                            Bytes = ms.ToArray(),
                                            Connection = connection
                                        }));
                                    }
                                    break;
                                case WebSocketMessageType.Binary:
                                    FireEvent(this, CreateMessageEventArgs(new WSMessageServerBaseEventArgs<Z>
                                    {
                                        MessageEventType = MessageEventType.Receive,
                                        Bytes = ms.ToArray(),
                                        Connection = connection
                                    }));
                                    break;
                                case WebSocketMessageType.Close:
                                    await DisconnectConnectionAsync(connection, WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken).ConfigureAwait(false);
                                    break;
                                default:
                                    break;
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
                    }));
                }
            }


            FireEvent(this, CreateConnectionEventArgs(new WSConnectionServerBaseEventArgs<Z>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Disconnect,
            }));
        }

        protected virtual async Task<bool> CanUpgradeConnection(string message, string[] requestedSubprotocols, Dictionary<string, string> requestHeaders, Z connection, CancellationToken cancellationToken)
        {
            SetPathAndQueryStringForConnection(message, connection);

            if (requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0)
            {
                if (!AreSubprotocolsRequestedValid(requestedSubprotocols))
                {
                    var bytes = Encoding.UTF8.GetBytes("Invalid subprotocols requested");
                    await connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                    await DisconnectConnectionAsync(connection, WebSocketCloseStatus.ProtocolError, "Invalid Subprotocol", cancellationToken).ConfigureAwait(false);
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
            connection.Websocket = WebSocket.CreateFromStream(connection.TcpClient.GetStream(), true, subProtocol, TimeSpan.FromSeconds(_parameters.PingIntervalSec));

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                $"{HttpKnownHeaderNames.Connection}: Upgrade\r\n" +
                $"{HttpKnownHeaderNames.Upgrade}: websocket\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketAccept}: {swkaSha1Base64}\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketProtocol}: {subProtocol}\r\n\r\n");

            await connection.TcpClient.Client.SendAsync(new ArraySegment<byte>(response), SocketFlags.None, cancellationToken).ConfigureAwait(false);
            
            if (!_parameters.OnlyEmitBytes && !string.IsNullOrWhiteSpace(_parameters.ConnectionSuccessString))
            {
                await SendAsync(_parameters.ConnectionSuccessString, connection, cancellationToken).ConfigureAwait(false);
            }

            FireEvent(this, CreateConnectionEventArgs(new WSConnectionServerBaseEventArgs<Z>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Connected,
                RequestHeaders = requestHeaders
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
                connection.Path = pathAndQueryString.StartsWith("/") ? pathAndQueryString.Substring(1, pathAndQueryString.IndexOf("?") - 1) : pathAndQueryString.Substring(0, pathAndQueryString.IndexOf("?") - 1);

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
                connection.Path = pathAndQueryString;
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

        protected abstract U CreateMessageEventArgs(WSMessageServerBaseEventArgs<Z> args);
    }
}