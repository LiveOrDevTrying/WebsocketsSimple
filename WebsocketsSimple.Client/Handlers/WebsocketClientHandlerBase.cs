using PHS.Networking.Enums;
using PHS.Networking.Handlers;
using PHS.Networking.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client.Models
{
    public abstract class WebsocketClientHandlerBase<T, U, V, W, Y> :
        HandlerClientBase<T, U, V, W, Y>
        where T : WSConnectionEventArgs<Y>
        where U : WSMessageEventArgs<Y>
        where V : WSErrorEventArgs<Y>
        where W : ParamsWSClient
        where Y : ConnectionWS
    {
        public WebsocketClientHandlerBase(W parameters) : base(parameters)
        {
            _isRunning = true;
        }

        public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (_connection != null)
                    {
                        await DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }

                    _isRunning = true;

                    if (_parameters.IsWebsocketSecured)
                    {
                        await CreateSSLConnectionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        CreateConnection();
                    }

                    var uri = new Uri(ConstructURI());

                    // Create the security key and expected response, then build all of the request headers
                    (var secKey, var webSocketAccept) = CreateSecKeyAndSecWebSocketAccept();
                    var requestHeader = BuildRequestHeader(secKey, uri);

                    var buffer = new ArraySegment<byte>(requestHeader);
                    // Write out the header to the connection
                    if (Connection.SslStream != null)
                    {
                        await _connection.SslStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await _connection.TcpClient.Client.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                    }

                    if (_connection != null && _connection.TcpClient.Connected && !cancellationToken.IsCancellationRequested)
                    {
                        byte[][] remainingMessages;
                        string subprotocol;

                        if (_connection.SslStream != null)
                        {
                            (subprotocol, remainingMessages) = await ParseAndValidateConnectResponseSSLAsync(_connection, webSocketAccept, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            (subprotocol, remainingMessages) = await ParseAndValidateConnectResponseAsync(_connection, webSocketAccept, cancellationToken).ConfigureAwait(false);
                        }

                        if (_connection.TcpClient.Connected && remainingMessages != null && !cancellationToken.IsCancellationRequested)
                        {
                            if (_connection.SslStream != null)
                            {
                                _connection.Websocket = WebSocket.CreateClientWebSocket(_connection.SslStream,
                                    subprotocol,
                                    _parameters.ReceiveBufferSize,
                                    _parameters.SendBufferSize,
                                    _parameters.KeepAliveInterval,
                                    false,
                                    WebSocket.CreateClientBuffer(_parameters.ReceiveBufferSize, _parameters.SendBufferSize));
                            }
                            else
                            {
                                _connection.Websocket = WebSocket.CreateClientWebSocket(_connection.TcpClient.GetStream(),
                                    subprotocol,
                                    _parameters.ReceiveBufferSize,
                                    _parameters.SendBufferSize,
                                    _parameters.KeepAliveInterval,
                                    false,
                                    WebSocket.CreateClientBuffer(_parameters.ReceiveBufferSize, _parameters.SendBufferSize));
                            }

                            if (_connection.Websocket.State == WebSocketState.Open)
                            {
                                FireEvent(this, CreateConnectionEventArgs(new WSConnectionEventArgs<Y>
                                {
                                    ConnectionEventType = ConnectionEventType.Connected,
                                    Connection = _connection,
                                    CancellationToken = cancellationToken
                                }));

                                foreach (var bytes in remainingMessages)
                                {
                                    var message = Encoding.UTF8.GetString(bytes).Replace("\u0016", "");
                                    FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                                    {
                                        Bytes = bytes,
                                        Message = message,
                                        Connection = _connection,
                                        MessageEventType = MessageEventType.Receive,
                                        CancellationToken = cancellationToken
                                    }));
                                }

                                _ = Task.Run(async () => { await ReceiveAsync(cancellationToken).ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);

                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorEventArgs<Y>
                {
                    Exception = ex,
                    Message = $"Error during ConnectAsync() - {ex.Message}",
                    Connection = _connection,
                    CancellationToken = cancellationToken
                }));
            }

            await DisconnectAsync(cancellationToken).ConfigureAwait(false);

            return false;
        }

        public override async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return await DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Disconnection", cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string closeStatusDescription = "Disconnect",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (_connection != null && !cancellationToken.IsCancellationRequested)
                {
                    if (!_connection.Disposed)
                    {
                        _connection.Disposed = true;

                        if (_connection.Websocket != null &&
                            (_connection.Websocket.State == WebSocketState.Open ||
                            _connection.Websocket.State == WebSocketState.CloseReceived ||
                            _connection.Websocket.State == WebSocketState.CloseSent))
                        {
                            await _connection.Websocket.CloseAsync(webSocketCloseStatus, closeStatusDescription, cancellationToken).ConfigureAwait(false);
                        }

                        _isRunning = false;

                        _connection?.Dispose();

                        FireEvent(this, CreateConnectionEventArgs(new WSConnectionEventArgs<Y>
                        {
                            ConnectionEventType = ConnectionEventType.Disconnect,
                            Connection = _connection,
                            CancellationToken = cancellationToken
                        }));

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorEventArgs<Y>
                {
                    Exception = ex,
                    Message = $"Error in DisconnectAsync() - {ex.Message}",
                    Connection = _connection,
                    CancellationToken = cancellationToken
                }));
            }

            _isRunning = false;

            return false;
        }

        public override async Task<bool> SendAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open &&
                    !cancellationToken.IsCancellationRequested &&
                    _isRunning &&
                    !string.IsNullOrWhiteSpace(message))
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await _connection.Websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);

                    FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                    {
                        MessageEventType = MessageEventType.Sent,
                        Bytes = bytes,
                        Message = message,
                        Connection = _connection,
                        CancellationToken = cancellationToken
                    }));
                }

                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorEventArgs<Y>
                {
                    Exception = ex,
                    Message = $"Error during SendAsync() - {ex.Message}",
                    Connection = _connection,
                    CancellationToken = cancellationToken
                }));
            }

            await DisconnectAsync(cancellationToken).ConfigureAwait(false);

            return false;
        }
        public override async Task<bool> SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open &&
                    _isRunning &&
                    !cancellationToken.IsCancellationRequested &&
                    message.Where(x => x != 0).Any())
                {
                    await _connection.Websocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);

                    FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                    {
                        MessageEventType = MessageEventType.Sent,
                        Bytes = message,
                        Connection = _connection,
                        CancellationToken = cancellationToken
                    }));
                }

                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorEventArgs<Y>
                {
                    Exception = ex,
                    Message = $"Error during SendAsync() - {ex.Message}",
                    Connection = _connection,
                    CancellationToken = cancellationToken

                }));
            }

            return false;
        }

        protected virtual async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _connection != null && _connection.TcpClient.Connected)
                {
                    WebSocketReceiveResult result = null;

                    do
                    {
                        try
                        {
                            if (_connection.TcpClient.Available > 0)
                            {
                                var buffer = WebSocket.CreateClientBuffer(_parameters.ReceiveBufferSize, _parameters.SendBufferSize);
                                result = await _connection.Websocket.ReceiveAsync(buffer, cancellationToken);
                                await _connection.MemoryStream.WriteAsync(buffer.Array.AsMemory(buffer.Offset, result.Count), cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch { }
                    }
                    while (_connection != null && _connection.TcpClient.Connected && _connection.Websocket.State == WebSocketState.Open && result != null && !result.EndOfMessage);

                    if (_connection != null && result != null && result.EndOfMessage)
                    {
                        var bytes = _connection.MemoryStream.ToArray();
                        _connection.MemoryStream.SetLength(0);

                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                                {
                                    Bytes = bytes,
                                    Message = Encoding.UTF8.GetString(bytes, 0, bytes.Length),
                                    Connection = _connection,
                                    MessageEventType = MessageEventType.Receive,
                                    CancellationToken = cancellationToken
                                }));
                                break;
                            case WebSocketMessageType.Binary:
                                FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                                {
                                    Bytes = bytes,
                                    Connection = _connection,
                                    MessageEventType = MessageEventType.Receive,
                                    CancellationToken = cancellationToken
                                }));
                                break;
                            case WebSocketMessageType.Close:
                                await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorEventArgs<Y>
                {
                    Exception = ex,
                    Message = $"Error in ReceiveAsync() - {ex.Message}",
                    Connection = _connection,
                    CancellationToken = cancellationToken
                }));
            }

            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        protected virtual async Task ReceiveSSLAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _connection != null && _connection.TcpClient.Connected)
                {
                    WebSocketReceiveResult result = null;

                    do
                    {
                        try
                        {
                            var bytesRead = 0;
                            if ((bytesRead = _connection.SslStream.Read(_connection.ReadBuffer, 0, _connection.ReadBuffer.Length)) > 0)
                            {
                                await _connection.MemoryStream.WriteAsync(_connection.ReadBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                                _connection.ReadBuffer = new byte[4096];
                            }
                            else
                            {
                                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch { }
                    }
                    while (_connection != null && _connection.TcpClient.Connected && _connection.Websocket.State == WebSocketState.Open && result != null && !result.EndOfMessage);

                    if (_connection != null && result != null && result.EndOfMessage)
                    {
                        var bytes = _connection.MemoryStream.ToArray();
                        _connection.MemoryStream.SetLength(0);

                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                                {
                                    Bytes = bytes,
                                    Message = Encoding.UTF8.GetString(bytes, 0, bytes.Length),
                                    Connection = _connection,
                                    MessageEventType = MessageEventType.Receive,
                                    CancellationToken = cancellationToken
                                }));
                                break;
                            case WebSocketMessageType.Binary:
                                FireEvent(this, CreateMessageEventArgs(new WSMessageEventArgs<Y>
                                {
                                    Bytes = bytes,
                                    Connection = _connection,
                                    MessageEventType = MessageEventType.Receive,
                                    CancellationToken = cancellationToken
                                }));
                                break;
                            case WebSocketMessageType.Close:
                                await DisconnectAsync(cancellationToken).ConfigureAwait(false);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, CreateErrorEventArgs(new WSErrorEventArgs<Y>
                {
                    Exception = ex,
                    Message = $"Error in ReceiveAsync() - {ex.Message}",
                    Connection = _connection,
                    CancellationToken = cancellationToken
                }));
            }

            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        protected virtual void CreateConnection()
        {
            _connection?.Dispose();
            _connection = null;

            var client = new TcpClient(_parameters.Host, _parameters.Port)
            {
                ReceiveTimeout = 60000
            };

            _connection = CreateConnection(new ConnectionWS 
            {
                TcpClient = client
            });
        }
        protected virtual async Task CreateSSLConnectionAsync(CancellationToken cancellationToken)
        {
            _connection?.Dispose();
            _connection = null;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var client = new TcpClient(_parameters.Host, _parameters.Port)
            {
                ReceiveTimeout = 60000
            };

            var clientCertificates = new X509Certificate2Collection();
            if (_parameters.ClientCertificates != null)
            {
                for (int i = 0; i < _parameters.ClientCertificates.Length; i++)
                {
                    clientCertificates.Add(_parameters.ClientCertificates[i]);
                }
            }

            var sslStream = new SslStream(client.GetStream());
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "connect.themonitaur.com",
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = _parameters.EnabledSslProtocols,
                //CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            }, cancellationToken);

            if (sslStream.IsAuthenticated && sslStream.IsEncrypted && !cancellationToken.IsCancellationRequested)
            {
                _connection = CreateConnection(new ConnectionWS
                {
                    TcpClient = client,
                    SslStream = sslStream,
                    ReadBuffer = new byte[4096]
                });
            }
            else
            {
                throw new Exception("Could not create connection - SSL cert has validation problem.");
            }
        }
        
        protected abstract Y CreateConnection(ConnectionWS connection);
        protected abstract T CreateConnectionEventArgs(WSConnectionEventArgs<Y> args);
        protected abstract U CreateMessageEventArgs(WSMessageEventArgs<Y> args);
        protected abstract V CreateErrorEventArgs(WSErrorEventArgs<Y> args);

        protected virtual string ConstructURI()
        {
            var prefix = _parameters.IsWebsocketSecured ? "wss" : "ws";

            var path = _parameters.Path == null ? "/" : !_parameters.Path.StartsWith("/") ? $"/{_parameters.Path}" : _parameters.Path;
            var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);

            if (_parameters.QueryStringParameters != null)
            {
                foreach (var item in _parameters.QueryStringParameters)
                {
                    if (item.Key == "token" && !string.IsNullOrWhiteSpace(_parameters.Token))
                    {
                        throw new Exception("Duplicate query string parameter - token. Please remove from QueryStringParameters");
                    }

                    qs.Add(item.Key, item.Value);
                }
            }

            var fullPath = new StringBuilder();
            fullPath.Append($"{prefix}://{_parameters.Host}");

            if ((_parameters.IsWebsocketSecured && _parameters.Port != 0 && _parameters.Port != 443) ||
                !_parameters.IsWebsocketSecured && _parameters.Port != 0 && _parameters.Port != 80)
            {
                fullPath.Append($":{_parameters.Port}");
            }

            fullPath.Append(path);

            if (qs.Count > 0)
            {
                fullPath.Append($"?{qs.ToString()}");
            }

            return fullPath.ToString();
        }
        protected virtual (string, string) CreateSecKeyAndSecWebSocketAccept()
        {
            var secKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            using (var sha = SHA1.Create())
            {
                return (
                    secKey,
                    Convert.ToBase64String(
                        sha.ComputeHash(Encoding.ASCII.GetBytes(secKey + Statics.WS_SERVER_GUID))
                    )
                );
            }
        }
        protected virtual byte[] BuildRequestHeader(string secKey, Uri uri)
        {
            var builder = new StringBuilder()
                .Append("GET ")
                .Append(uri.PathAndQuery)
                .Append(" HTTP/1.1\r\n");

            // Add all of the required headers, honoring Host header if set.
            var hostHeader = string.Empty;
            if (_parameters.RequestHeaders != null &&
                !_parameters.RequestHeaders.TryGetValue(HttpKnownHeaderNames.Host, out hostHeader))
            {
                hostHeader = uri.Host;
            }

            builder.Append($"{HttpKnownHeaderNames.Host}: ");
            if (string.IsNullOrWhiteSpace(hostHeader))
            {
                builder.Append(uri.IdnHost).Append(':').Append(uri.Port).Append("\r\n");
            }
            else
            {
                builder.Append(hostHeader).Append("\r\n");
            }

            builder.Append($"{HttpKnownHeaderNames.Connection}: Upgrade\r\n");
            builder.Append($"{HttpKnownHeaderNames.Upgrade}: websocket\r\n");
            builder.Append($"{HttpKnownHeaderNames.SecWebSocketVersion}: 13\r\n");
            builder.Append($"{HttpKnownHeaderNames.SecWebSocketKey}: ").Append(secKey).Append("\r\n");

            // Add all of the additionally requested headers
            if (_parameters.RequestHeaders != null)
            {
                foreach (string key in _parameters.RequestHeaders.Keys)
                {
                    if (string.Equals(key, HttpKnownHeaderNames.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        // Host header handled above
                        continue;
                    }

                    builder.Append(key).Append(": ").Append(_parameters.RequestHeaders[key]).Append("\r\n");
                }
            }

            if (!string.IsNullOrWhiteSpace(_parameters.Token))
            {
                builder.Append($"{HttpKnownHeaderNames.Authorization}:Bearer {_parameters.Token}\r\n");
            } 

            // Add the optional subprotocols header
            if (_parameters.RequestedSubProtocols != null &&
                _parameters.RequestedSubProtocols.Length > 0)
            {
                builder.Append(HttpKnownHeaderNames.SecWebSocketProtocol).Append(": ");
                builder.Append(_parameters.RequestedSubProtocols[0]);
                for (int i = 1; i < _parameters.RequestedSubProtocols.Length; i++)
                {
                    builder.Append(", ").Append(_parameters.RequestedSubProtocols[i]);
                }
                builder.Append("\r\n");
            }

            // End the headers
            builder.Append("\r\n");

            // Return the bytes for the built up header
            return Encoding.ASCII.GetBytes(builder.ToString());
        }
        protected async Task<(string, byte[][])> ParseAndValidateConnectResponseAsync(ConnectionWS connection, string expectedSecWebSocketAccept, CancellationToken cancellationToken)
        {
            while (connection.TcpClient.Connected && !cancellationToken.IsCancellationRequested)
            {
                if (connection.TcpClient.Available > 0)
                {
                    break;
                }

                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }

            if (!connection.TcpClient.Connected || cancellationToken.IsCancellationRequested)
            {
                await DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return (null, null);
            }

            var readBuffer = new byte[connection.TcpClient.Available];
            var buffer = new ArraySegment<byte>(readBuffer);
            await connection.TcpClient.Client.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);

            var message = Encoding.UTF8.GetString(buffer);

            var messagesSplit = message.Split("\r\n");

            if (messagesSplit.Length <= 0)
            {
                throw new WebSocketException("Not valid handshake");
            }

            // Depending on the underlying sockets implementation and timing, connecting to a server that then
            // immediately closes the connection may either result in an exception getting thrown from the connect
            // earlier, or it may result in getting to here but reading 0 bytes.  If we read 0 bytes and thus have
            // an empty status line, treat it as a connect failure.
            if (string.IsNullOrEmpty(messagesSplit[0]))
            {
                throw new WebSocketException("Connection failure.");
            }

            const string ExpectedStatusStart = "HTTP/1.1 ";
            const string ExpectedStatusStatWithCode = "HTTP/1.1 101"; // 101 == SwitchingProtocols

            // If the status line doesn't begin with "HTTP/1.1" or isn't long enough to contain a status code, fail.
            if (!messagesSplit[0].StartsWith(ExpectedStatusStart, StringComparison.Ordinal) || messagesSplit[0].Length < ExpectedStatusStatWithCode.Length)
            {
                throw new WebSocketException(WebSocketError.HeaderError, $"Connection failure (status line = '{messagesSplit[0]}').");
            }

            // If the status line doesn't contain a status code 101, or if it's long enough to have a status description
            // but doesn't contain whitespace after the 101, fail.
            if (!messagesSplit[0].StartsWith(ExpectedStatusStatWithCode, StringComparison.Ordinal) ||
                (messagesSplit[0].Length > ExpectedStatusStatWithCode.Length && !char.IsWhiteSpace(messagesSplit[0][ExpectedStatusStatWithCode.Length])))
            {
                throw new WebSocketException(WebSocketError.HeaderError, $"Connection failure (status line = '{messagesSplit[0]}').");
            }

            // Read each response header. Be liberal in parsing the response header, treating
            // everything to the left of the colon as the key and everything to the right as the value, trimming both.
            // For each header, validate that we got the expected value.
            bool foundUpgrade = false, foundConnection = false, foundSecWebSocketAccept = false;
            string subprotocol = null;
            var remainingMessages = new List<byte[]>(); ;
            for (int i = 1; i < messagesSplit.Length; i++)
            {
                if (string.IsNullOrEmpty(messagesSplit[i]) && messagesSplit.Length >= i + 1)
                {
                    for (int j = i + 1; j < messagesSplit.Length; j++)
                    {
                        var bytes = ConvertUTF8ToASCIIBytes(messagesSplit[j].Trim());
                        if (bytes.Length > 0)
                        {
                            remainingMessages.Add(bytes);
                        }
                    }

                    break;
                }

                var colonIndex = messagesSplit[i].IndexOf(':');
                if (colonIndex == -1)
                {
                    throw new WebSocketException(WebSocketError.HeaderError);
                }

                var headerName = SubstringTrim(messagesSplit[i], 0, colonIndex);
                var headerValue = SubstringTrim(messagesSplit[i], colonIndex + 1);

                // The Connection, Upgrade, and SecWebSocketAccept headers are required and with specific values.
                ValidateAndTrackHeader(HttpKnownHeaderNames.Connection, "Upgrade", headerName, headerValue, ref foundConnection);
                ValidateAndTrackHeader(HttpKnownHeaderNames.Upgrade, "websocket", headerName, headerValue, ref foundUpgrade);
                ValidateAndTrackHeader(HttpKnownHeaderNames.SecWebSocketAccept, expectedSecWebSocketAccept, headerName, headerValue, ref foundSecWebSocketAccept);

                // The SecWebSocketProtocol header is optional.  We should only get it with a non-empty value if we requested subprotocols,
                // and then it must only be one of the ones we requested.  If we got a subprotocol other than one we requested (or if we
                // already got one in a previous header), fail. Otherwise, track which one we got.
                if (string.Equals(HttpKnownHeaderNames.SecWebSocketProtocol, headerName, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(headerValue))
                {
                    if (_parameters.RequestedSubProtocols == null)
                    {
                        throw new WebSocketException("Requested sub protocols cannot be empty if server returns sub protocol");
                    }

                    var newSubprotocol = _parameters.RequestedSubProtocols.ToList().Find(requested => string.Equals(requested, headerValue, StringComparison.OrdinalIgnoreCase));
                    if (newSubprotocol == null || subprotocol != null)
                    {
                        throw new WebSocketException(
                            string.Format("Unsupported sub-protocol '{0}' (expected one of [{1}]).",
                                subprotocol,
                                string.Join(", ", _parameters.RequestedSubProtocols)
                            )
                        );
                    }
                    subprotocol = newSubprotocol;
                }
            }
            if (!foundUpgrade || !foundConnection || !foundSecWebSocketAccept)
            {
                throw new WebSocketException("Connection failure.");
            }

            return (subprotocol, remainingMessages.ToArray());
        }
        protected async Task<(string, byte[][])> ParseAndValidateConnectResponseSSLAsync(ConnectionWS connection, string expectedSecWebSocketAccept, CancellationToken cancellationToken)
        {
            while (connection.TcpClient.Connected && !cancellationToken.IsCancellationRequested)
            {
                var bytesRead = 0;
                if ((bytesRead = connection.SslStream.Read(connection.ReadBuffer, 0, connection.ReadBuffer.Length)) > 0)
                {
                    await connection.MemoryStream.WriteAsync(connection.ReadBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    connection.ReadBuffer = new byte[4096];
                }

                if (bytesRead > 0)
                {
                    break;
                }
                else
                {
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!connection.TcpClient.Connected || cancellationToken.IsCancellationRequested)
            {
                await DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return (null, null);
            }

            var message = Encoding.UTF8.GetString(connection.MemoryStream.ToArray());
            connection.MemoryStream.SetLength(0);

            var messagesSplit = message.Split("\r\n");

            if (messagesSplit.Length <= 0)
            {
                throw new WebSocketException("Not valid handshake");
            }

            // Depending on the underlying sockets implementation and timing, connecting to a server that then
            // immediately closes the connection may either result in an exception getting thrown from the connect
            // earlier, or it may result in getting to here but reading 0 bytes.  If we read 0 bytes and thus have
            // an empty status line, treat it as a connect failure.
            if (string.IsNullOrEmpty(messagesSplit[0]))
            {
                throw new WebSocketException("Connection failure.");
            }

            const string ExpectedStatusStart = "HTTP/1.1 ";
            const string ExpectedStatusStatWithCode = "HTTP/1.1 101"; // 101 == SwitchingProtocols

            // If the status line doesn't begin with "HTTP/1.1" or isn't long enough to contain a status code, fail.
            if (!messagesSplit[0].StartsWith(ExpectedStatusStart, StringComparison.Ordinal) || messagesSplit[0].Length < ExpectedStatusStatWithCode.Length)
            {
                throw new WebSocketException(WebSocketError.HeaderError, $"Connection failure (status line = '{messagesSplit[0]}').");
            }

            // If the status line doesn't contain a status code 101, or if it's long enough to have a status description
            // but doesn't contain whitespace after the 101, fail.
            if (!messagesSplit[0].StartsWith(ExpectedStatusStatWithCode, StringComparison.Ordinal) ||
                (messagesSplit[0].Length > ExpectedStatusStatWithCode.Length && !char.IsWhiteSpace(messagesSplit[0][ExpectedStatusStatWithCode.Length])))
            {
                throw new WebSocketException(WebSocketError.HeaderError, $"Connection failure (status line = '{messagesSplit[0]}').");
            }

            // Read each response header. Be liberal in parsing the response header, treating
            // everything to the left of the colon as the key and everything to the right as the value, trimming both.
            // For each header, validate that we got the expected value.
            bool foundUpgrade = false, foundConnection = false, foundSecWebSocketAccept = false;
            string subprotocol = null;
            var remainingMessages = new List<byte[]>(); ;
            for (int i = 1; i < messagesSplit.Length; i++)
            {
                if (string.IsNullOrEmpty(messagesSplit[i]) && messagesSplit.Length >= i + 1)
                {
                    for (int j = i + 1; j < messagesSplit.Length; j++)
                    {
                        var bytes = ConvertUTF8ToASCIIBytes(messagesSplit[j].Trim());
                        if (bytes.Length > 0)
                        {
                            remainingMessages.Add(bytes);
                        }
                    }

                    break;
                }

                var colonIndex = messagesSplit[i].IndexOf(':');
                if (colonIndex == -1)
                {
                    throw new WebSocketException(WebSocketError.HeaderError);
                }

                var headerName = SubstringTrim(messagesSplit[i], 0, colonIndex);
                var headerValue = SubstringTrim(messagesSplit[i], colonIndex + 1);

                // The Connection, Upgrade, and SecWebSocketAccept headers are required and with specific values.
                ValidateAndTrackHeader(HttpKnownHeaderNames.Connection, "Upgrade", headerName, headerValue, ref foundConnection);
                ValidateAndTrackHeader(HttpKnownHeaderNames.Upgrade, "websocket", headerName, headerValue, ref foundUpgrade);
                ValidateAndTrackHeader(HttpKnownHeaderNames.SecWebSocketAccept, expectedSecWebSocketAccept, headerName, headerValue, ref foundSecWebSocketAccept);

                // The SecWebSocketProtocol header is optional.  We should only get it with a non-empty value if we requested subprotocols,
                // and then it must only be one of the ones we requested.  If we got a subprotocol other than one we requested (or if we
                // already got one in a previous header), fail. Otherwise, track which one we got.
                if (string.Equals(HttpKnownHeaderNames.SecWebSocketProtocol, headerName, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(headerValue))
                {
                    if (_parameters.RequestedSubProtocols == null)
                    {
                        throw new WebSocketException("Requested sub protocols cannot be empty if server returns sub protocol");
                    }

                    var newSubprotocol = _parameters.RequestedSubProtocols.ToList().Find(requested => string.Equals(requested, headerValue, StringComparison.OrdinalIgnoreCase));
                    if (newSubprotocol == null || subprotocol != null)
                    {
                        throw new WebSocketException(
                            string.Format("Unsupported sub-protocol '{0}' (expected one of [{1}]).",
                                subprotocol,
                                string.Join(", ", _parameters.RequestedSubProtocols)
                            )
                        );
                    }
                    subprotocol = newSubprotocol;
                }
            }
            if (!foundUpgrade || !foundConnection || !foundSecWebSocketAccept)
            {
                throw new WebSocketException("Connection failure.");
            }

            return (subprotocol, remainingMessages.ToArray());
        }
        protected virtual byte[] ConvertUTF8ToASCIIBytes(string content)
        {
            return Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(Encoding.ASCII.EncodingName,
                new EncoderReplacementFallback(string.Empty),
                new DecoderExceptionFallback()),
                Encoding.UTF8.GetBytes(content));
        }
        protected virtual void ValidateAndTrackHeader(
           string targetHeaderName, string targetHeaderValue,
           string foundHeaderName, string foundHeaderValue,
           ref bool foundHeader)
        {
            var isTargetHeader = string.Equals(targetHeaderName, foundHeaderName, StringComparison.OrdinalIgnoreCase);
            if (!foundHeader)
            {
                if (isTargetHeader)
                {
                    if (!string.Equals(targetHeaderValue, foundHeaderValue, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new WebSocketException(
                            $"Invalid value for '{foundHeaderName}' header: '{foundHeaderValue}' (expected '{targetHeaderValue}')."
                        );
                    }
                    foundHeader = true;
                }
            }
            else
            {
                if (isTargetHeader)
                {
                    throw new WebSocketException("Connection failure.");
                }
            }
        }
        protected virtual string SubstringTrim(string text, int startIndex)
        {
            return SubstringTrim(text, startIndex, text.Length - startIndex);
        }
        protected virtual string SubstringTrim(string text, int startIndex, int length)
        {
            Debug.Assert(text != null, "string must be non-null");
            Debug.Assert(startIndex >= 0, "startIndex must be non-negative");
            Debug.Assert(length >= 0, "length must be non-negative");
            Debug.Assert(startIndex <= text.Length - length, "startIndex + length must be <= value.Length");

            if (length == 0)
            {
                return string.Empty;
            }

            int endIndex = startIndex + length - 1;

            while (startIndex <= endIndex && char.IsWhiteSpace(text[startIndex]))
            {
                startIndex++;
            }

            while (endIndex >= startIndex && char.IsWhiteSpace(text[endIndex]))
            {
                endIndex--;
            }

            var newLength = endIndex - startIndex + 1;
            Debug.Assert(newLength >= 0 && newLength <= text.Length, "Expected resulting length to be within value's length");

            return
                newLength == 0 ? string.Empty :
                newLength == text.Length ? text :
                text.Substring(startIndex, newLength);
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
