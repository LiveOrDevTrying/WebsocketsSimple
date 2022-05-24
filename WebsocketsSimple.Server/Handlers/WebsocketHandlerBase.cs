using PHS.Networking.Enums;
using PHS.Networking.Events;
using PHS.Networking.Server.Enums;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using System;
using System.Collections.Generic;
using System.IO;
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
    public abstract class WebsocketHandlerBase<T> :
        CoreNetworking<WSConnectionServerBaseEventArgs<T>, WSMessageServerBaseEventArgs<T>, WSErrorServerBaseEventArgs<T>>,
        ICoreNetworking<WSConnectionServerBaseEventArgs<T>, WSMessageServerBaseEventArgs<T>, WSErrorServerBaseEventArgs<T>>
        where T : ConnectionWSServer
    {
        protected readonly byte[] _certificate;
        protected readonly string _certificatePassword;
        protected readonly ParamsWSServer _parameters;
        protected TcpListener _server;
        protected volatile bool _isRunning;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketHandlerBase(ParamsWSServer parameters)
        {
            _parameters = parameters;
        }
        public WebsocketHandlerBase(ParamsWSServer parameters, byte[] certificate, string certificatePassword)
        {
            _parameters = parameters;
            _certificate = certificate;
            _certificatePassword = certificatePassword;
        }

        public virtual bool Start(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_server != null)
                {
                    Stop();
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
                    _ = Task.Run(async () => { await ListenForConnectionsAsync(cancellationToken); }, cancellationToken);
                }
                else
                {
                    _ = Task.Run(async () => { await ListenForConnectionsSSLAsync(cancellationToken); }, cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerBaseEventArgs<T>
                {
                    Exception = ex,
                    Message = ex.Message,
                });
            }

            return false;
        }
        public virtual void Stop()
        {
            _isRunning = false;

            try
            {
                if (_server != null)
                {
                    _server.Stop();
                    _server = null;

                    FireEvent(this, new ServerEventArgs
                    {
                        ServerEventType = ServerEventType.Stop
                    });
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerBaseEventArgs<T>
                {
                    Exception = ex,
                    Message = ex.Message,
                });
            }
        }

        protected abstract T CreateConnection(TcpClient client, Stream stream);

        protected virtual async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync(cancellationToken);
                    var stream = client.GetStream();

                    var connection = CreateConnection(client, stream);

                    _ = Task.Run(async () => { await StartReceivingMessagesAsync(connection, cancellationToken); }, cancellationToken);
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerBaseEventArgs<T>
                    {
                        Exception = ex,
                        Message = ex.Message,
                    });
                }

            }
        }
        protected virtual async Task ListenForConnectionsSSLAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync(cancellationToken);
                    var sslStream = new SslStream(client.GetStream());
                    await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = new X509Certificate2(_certificate, _certificatePassword)
                    }, cancellationToken);

                    if (sslStream.IsAuthenticated && sslStream.IsEncrypted)
                    {
                        var connection = CreateConnection(client, sslStream);

                        _ = Task.Run(async () => { await StartReceivingMessagesAsync(connection, cancellationToken); }, cancellationToken);
                    }
                    else
                    {
                        var certStatus = $"IsAuthenticated = {sslStream.IsAuthenticated} && IsEncrypted == {sslStream.IsEncrypted}";
                        FireEvent(this, new WSErrorServerBaseEventArgs<T>
                        {
                            Exception = new Exception(certStatus),
                            Message = certStatus
                        });

                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerBaseEventArgs<T>
                    {
                        Exception = ex,
                        Message = ex.Message,
                    });
                }

            }
        }
        protected virtual async Task StartReceivingMessagesAsync(T connection, CancellationToken cancellationToken)
        {
            try
            {
                while (connection.Client.Connected && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1, cancellationToken);
                    if (connection.Client.Available < 3)
                    {
                        continue;
                    }; // match against "get"

                    if (connection.Websocket == null)
                    {
                        var bytes = new byte[connection.Client.Available];

                        await connection.Stream.ReadAsync(bytes, 0, connection.Client.Available, cancellationToken);

                        var data = Encoding.UTF8.GetString(bytes);

                        if (Regex.IsMatch(data, "^GET", RegexOptions.IgnoreCase))
                        {
                            var requestedSubprotocols = Regex.Match(data, $"{HttpKnownHeaderNames.SecWebSocketProtocol}: (.*)").Groups[1].Value.Trim().Split(",");
                            if (await CanUpgradeConnection(data, requestedSubprotocols, connection, cancellationToken))
                            {
                                await UpgradeConnectionAsync(data, requestedSubprotocols, connection, cancellationToken);
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
                                var bytes = new byte[connection.Client.Available];
                                result = await connection.Websocket.ReceiveAsync(new ArraySegment<byte>(bytes), cancellationToken);
                                ms.Write(TrimEnd(bytes));
                            } while (!result.EndOfMessage);

                            switch (result.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    var message = Encoding.UTF8.GetString(ms.ToArray());

                                    if (!string.IsNullOrWhiteSpace(message))
                                    {
                                        if (message.Trim().ToLower() == "pong")
                                        {
                                            connection.PingAttempts = 0;
                                        }
                                        else
                                        {
                                            FireEvent(this, new WSMessageServerBaseEventArgs<T>
                                            {
                                                MessageEventType = MessageEventType.Receive,
                                                Message = message,
                                                Bytes = ms.ToArray(),
                                                Connection = connection
                                            });
                                        }
                                    }
                                    break;
                                case WebSocketMessageType.Binary:
                                    FireEvent(this, new WSMessageServerBaseEventArgs<T>
                                    {
                                        MessageEventType = MessageEventType.Receive,
                                        Bytes = ms.ToArray(),
                                        Connection = connection
                                    });
                                    break;
                                case WebSocketMessageType.Close:
                                    await ReceiveDisconnectConnectionAsync(connection, cancellationToken);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch
            { }

            FireEvent(this, new WSConnectionServerBaseEventArgs<T>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Disconnect,
            });
        }

        protected virtual async Task<bool> CanUpgradeConnection(string message, string[] requestedSubprotocols, T connection, CancellationToken cancellationToken)
        {
            SetPathAndQueryStringForConnection(message, connection);

            if (requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0)
            {
                if (!AreSubprotocolsRequestedValid(requestedSubprotocols))
                {
                    var bytes = Encoding.UTF8.GetBytes("Invalid subprotocols requested");
                    await connection.Stream.WriteAsync(bytes, cancellationToken);
                    await DisconnectConnectionAsync(connection, cancellationToken);
                    return false;
                }
            }

            return true;
        }
        protected virtual async Task UpgradeConnectionAsync(string message, string[] requestedSubprotocols, T connection, CancellationToken cancellationToken)
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
            connection.Websocket = WebSocket.CreateFromStream(connection.Stream, true, subProtocol, WebSocket.DefaultKeepAliveInterval);

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                $"{HttpKnownHeaderNames.Connection}: Upgrade\r\n" +
                $"{HttpKnownHeaderNames.Upgrade}: websocket\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketAccept}: {swkaSha1Base64}\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketProtocol}: {subProtocol}\r\n\r\n");

            await connection.Stream.WriteAsync(response, 0, response.Length, cancellationToken);

            await SendAsync(_parameters.ConnectionSuccessString, connection, cancellationToken);

            FireEvent(this, new WSConnectionServerBaseEventArgs<T>
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Connected,
            });
        }
        protected virtual void SetPathAndQueryStringForConnection(string message, T connection)
        {
            // Get Path and QueryStrung and load into connection
            var split = message.Split(" ");

            if (split.Length < 2)
            {
                return;
            }

            var pathAndQueryString = split[1];
            connection.Path = pathAndQueryString.StartsWith("/") ? pathAndQueryString.Substring(1, pathAndQueryString.IndexOf("?") - 1) : pathAndQueryString.Substring(0, pathAndQueryString.IndexOf("?") - 1);

            var qsParsedToken = HttpUtility.ParseQueryString(pathAndQueryString.Substring(pathAndQueryString.IndexOf("?") + 1));

            var kvps = new List<KeyValuePair<string, string>>();
            foreach (string item in qsParsedToken)
            {
                kvps.Add(new KeyValuePair<string, string>(item, qsParsedToken[item]));
            }
            connection.QueryStringParameters = kvps.ToArray();
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

        public virtual async Task<bool> SendAsync(string message, T connection, CancellationToken cancellationToken)
        {
            try
            {
                if (connection.Client != null && connection.Client.Connected && connection.Websocket != null && connection.Websocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: bytes,
                        offset: 0,
                        count: message.Length),
                        messageType: WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: cancellationToken);

                    FireEvent(this, new WSMessageServerBaseEventArgs<T>
                    {
                        MessageEventType = MessageEventType.Sent,
                        Message = message,
                        Bytes = bytes,
                        Connection = connection,
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerBaseEventArgs<T>
                {
                    Exception = ex,
                    Message = ex.Message,
                    Connection = connection,
                });

            }

            return false;
        }
        public virtual async Task<bool> SendAsync(byte[] message, T connection, CancellationToken cancellationToken)
        {
            try
            {
                if (connection.Client != null && connection.Client.Connected && connection.Websocket != null && connection.Websocket.State == WebSocketState.Open)
                {
                    await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: message,
                        offset: 0,
                        count: message.Length),
                        messageType: WebSocketMessageType.Binary,
                        endOfMessage: true,
                        cancellationToken: cancellationToken);

                    FireEvent(this, new WSMessageServerBaseEventArgs<T>
                    {
                        MessageEventType = MessageEventType.Sent,
                        Bytes = message,
                        Connection = connection,
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerBaseEventArgs<T>
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                });

                await DisconnectConnectionAsync(connection, cancellationToken);
            }

            return false;
        }
        protected virtual async Task ReceiveDisconnectConnectionAsync(T connection, CancellationToken cancellationToken)
        {
            if (connection.Websocket != null && connection.Client != null && connection.Client.Connected)
            {
                try
                {
                    await connection.Websocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken);
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerBaseEventArgs<T>
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    });
                }

                try
                {
                    connection.Client.Close();
                    connection.Client.Dispose();
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerBaseEventArgs<T>
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    });
                }

                try
                {
                    connection.Stream.Close();
                    connection.Stream.Dispose();
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerBaseEventArgs<T>
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    });
                }
            }
        }
        public virtual async Task DisconnectConnectionAsync(T connection, CancellationToken cancellationToken)
        {
            if (connection.Websocket != null && connection.Client != null && connection.Client.Connected)
            {
                try
                {
                    await connection.Websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", cancellationToken);
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerBaseEventArgs<T>
                    {
                        Connection = connection,
                        Exception = ex,
                        Message = ex.Message
                    });
                }
            }
        }
        protected virtual byte[] TrimEnd(byte[] array)
        {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);

            Array.Resize(ref array, lastIndex + 1);

            return array;
        }
        protected virtual void FireEvent(object sender, ServerEventArgs args)
        {
            _serverEvent?.Invoke(sender, args);
        }

        public override void Dispose()
        {
            Stop();

            base.Dispose();
        }

        public TcpListener Server
        {
            get
            {
                return _server;
            }
        }
        public bool IsServerRunning
        {
            get
            {
                return _isRunning;
            }
        }

        public event NetworkingEventHandler<ServerEventArgs> ServerEvent
        {
            add
            {
                _serverEvent += value;
            }
            remove
            {
                _serverEvent -= value;
            }
        }
    }
}