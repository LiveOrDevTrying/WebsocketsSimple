using PHS.Networking.Enums;
using PHS.Networking.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Core;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client.Models
{
    public abstract class WebsocketClientHandlerBase<T> : 
        CoreNetworking<WSConnectionClientEventArgs, WSMessageClientEventArgs, WSErrorClientEventArgs>
        where T : ConnectionWS
    {
        protected readonly ParamsWSClient _parameters;
        protected readonly string _token;
        protected T _connection;

        public WebsocketClientHandlerBase(ParamsWSClient parameters, string token = "")
        {
            _parameters = parameters;
            _token = token;
        }

        public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (_connection != null)
                    {
                        await DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    }

                    if (_parameters.IsWebsocketSecured)
                    {
                        await CreateSSLConnectionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        CreateNonSSLConnection();
                    }

                    var uri = new Uri(ConstructURI());

                    // Create the security key and expected response, then build all of the request headers
                    (var secKey, var webSocketAccept) = CreateSecKeyAndSecWebSocketAccept();
                    var requestHeader = BuildRequestHeader(secKey, uri);

                    // Write out the header to the connection
                    await _connection.Stream.WriteAsync(requestHeader, 0, requestHeader.Length, cancellationToken).ConfigureAwait(false);

                    if (_connection.Client.Connected && !cancellationToken.IsCancellationRequested)
                    {
                        (var subprotocol, var remainingMessages) = await ParseAndValidateConnectResponseAsync(_connection, webSocketAccept, cancellationToken).ConfigureAwait(false);

                        if (_connection.Client.Connected && remainingMessages != null && !cancellationToken.IsCancellationRequested)
                        {
                            _connection.Websocket = WebSocket.CreateClientWebSocket(_connection.Stream,
                                subprotocol,
                                _parameters.ReceiveBufferSize,
                                _parameters.SendBufferSize,
                                _parameters.KeepAliveInterval,
                                false,
                                WebSocket.CreateClientBuffer(_parameters.ReceiveBufferSize, _parameters.SendBufferSize));

                            if (_connection.Websocket.State == WebSocketState.Open)
                            {
                                FireEvent(this, new WSConnectionClientEventArgs
                                {
                                    ConnectionEventType = ConnectionEventType.Connected,
                                    Connection = _connection
                                });

                                foreach (var bytes in remainingMessages)
                                {
                                    var message = Encoding.UTF8.GetString(bytes).Replace("\u0016", "");
                                    FireEvent(this, new WSMessageClientEventArgs
                                    {
                                        Bytes = bytes,
                                        Message = message,
                                        Connection = _connection,
                                        MessageEventType = MessageEventType.Receive
                                    });
                                }

                                _ = Task.Run(async () => { await ReceiveAsync(cancellationToken).ConfigureAwait(false); }, cancellationToken);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = $"Error during ConnectAsync() - {ex.Message}",
                    Connection = _connection
                });
            }

            await DisconnectAsync(cancellationToken: cancellationToken);
            return false;
        }
        public virtual async Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
            string closeStatusDescription = "Disconnect",
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested && 
                    _connection != null && 
                    _connection.Websocket != null && 
                        (_connection.Websocket.State == WebSocketState.Open ||
                        _connection.Websocket.State == WebSocketState.CloseReceived ||
                        _connection.Websocket.State == WebSocketState.CloseSent))
                {
                    await _connection.Websocket.CloseAsync(webSocketCloseStatus, closeStatusDescription, cancellationToken).ConfigureAwait(false);

                    FireEvent(this, new WSConnectionClientEventArgs
                    {
                        ConnectionEventType = ConnectionEventType.Disconnect,
                        Connection = _connection
                    });

                    _connection = null;

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = $"Error in DisconnectAsync() - {ex.Message}",
                    Connection = _connection
                });
            }

            return false;
        }

        public virtual async Task<bool> SendAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open &&
                    !cancellationToken.IsCancellationRequested)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await _connection.Websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);

                    FireEvent(this, new WSMessageClientEventArgs
                    {
                        MessageEventType = MessageEventType.Sent,
                        Bytes = bytes,
                        Message = message,
                        Connection = _connection,
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = $"Error during SendToServerAsync() - {ex.Message}",
                    Connection = _connection
                });
            }

            return false;
        }
        public virtual async Task<bool> SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_connection != null &&
                    _connection.Websocket != null &&
                    _connection.Websocket.State == WebSocketState.Open &&
                    !cancellationToken.IsCancellationRequested)
                {
                    await _connection.Websocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);

                    FireEvent(this, new WSMessageClientEventArgs
                    {
                        MessageEventType = MessageEventType.Sent,
                        Bytes = message,
                        Connection = _connection
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = $"Error during SendToServerAsync() - {ex.Message}",
                    Connection = _connection
                });
            }

            return false;
        }

        protected virtual async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _connection != null && _connection.Client.Connected)
                {
                    WebSocketReceiveResult result = null;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            if (_connection.Client.Available <= 0)
                            {
                                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                                continue;
                            };

                            var buffer = WebSocket.CreateClientBuffer(_parameters.ReceiveBufferSize, _parameters.SendBufferSize);
                            result = await _connection.Websocket.ReceiveAsync(buffer, cancellationToken);
                            await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count).ConfigureAwait(false);
                        } while (result == null || (!result.EndOfMessage && _connection != null && _connection.Client.Connected && _connection.Websocket.State == WebSocketState.Open));

                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                var message = Encoding.UTF8.GetString(ms.ToArray(), 0, result.Count);

                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    FireEvent(this, new WSMessageClientEventArgs
                                    {
                                        Bytes = ms.ToArray(),
                                        Message = message,
                                        Connection = _connection,
                                        MessageEventType = MessageEventType.Receive
                                    });
                                }
                                break;
                            case WebSocketMessageType.Binary:
                                FireEvent(this, new WSMessageClientEventArgs
                                {
                                    Bytes = ms.ToArray(),
                                    Connection = _connection,
                                    MessageEventType = MessageEventType.Receive
                                });
                                break;
                            case WebSocketMessageType.Close:
                                await DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorClientEventArgs
                {
                    Exception = ex,
                    Message = $"Error in ReceiveAsync() - {ex.Message}",
                    Connection = _connection
                });
            }
        }

        protected virtual void CreateNonSSLConnection()
        {
            var client = new TcpClient(_parameters.Host, _parameters.Port)
            {
                ReceiveTimeout = 60000
            };
            var stream = client.GetStream();

            _connection = CreateConnection(new ConnectionWS 
            {
                Client = client,
                Stream = stream
            });
        }
        protected virtual async Task CreateSSLConnectionAsync(CancellationToken cancellationToken)
        {
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
                TargetHost = _parameters.Host,
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = _parameters.EnabledSslProtocols,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            }, cancellationToken);

            if (sslStream.IsAuthenticated && sslStream.IsEncrypted && !cancellationToken.IsCancellationRequested)
            {
                _connection = CreateConnection(new ConnectionWS
                {
                    Client = client,
                    Stream = sslStream
                });
            }
            else
            {
                throw new Exception("Could not create connection - SSL cert has validation problem.");
            }
        }
        protected abstract T CreateConnection(ConnectionWS connection);

        protected virtual string ConstructURI()
        {
            var prefix = _parameters.IsWebsocketSecured ? "wss" : "ws";

            var path = _parameters.Path == null ? "/" : !_parameters.Path.StartsWith("/") ? $"/{_parameters.Path}" : _parameters.Path;
            var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrWhiteSpace(_token))
            {
                qs.Add("token", _token);
            }

            if (_parameters.QueryStringParameters != null)
            {
                foreach (var item in _parameters.QueryStringParameters)
                {
                    if (item.Key == "token" && !string.IsNullOrWhiteSpace(_token))
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
            while (connection.Client.Connected && !cancellationToken.IsCancellationRequested)
            {
                if (connection.Client.Available > 0)
                {
                    break;
                }

                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }

            if (!connection.Client.Connected || cancellationToken.IsCancellationRequested)
            {
                await DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return (null, null);
            }

            var readBuffer = new byte[connection.Client.Available];

            await connection.Stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false);

            var message = Encoding.UTF8.GetString(readBuffer);

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

            base.Dispose();
        }

        public T Connection
        {
            get
            {
                return _connection;
            }
        }

    }
}
