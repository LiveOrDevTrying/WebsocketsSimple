using Newtonsoft.Json;
using PHS.Networking.Enums;
using PHS.Networking.Events;
using PHS.Networking.Models;
using PHS.Networking.Server.Enums;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using System;
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
using WebsocketsSimple.Core;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public class WebsocketHandler : 
        CoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs>, 
        ICoreNetworking<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs> 
    {
        protected readonly byte[] _certificate;
        protected readonly string _certificatePassword;
        protected readonly IParamsWSServer _parameters;
        protected int _numberOfConnections;
        protected TcpListener _server;
        protected volatile bool _isRunning;

        private event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketHandler(IParamsWSServer parameters)
        {
            _parameters = parameters;
        }
        public WebsocketHandler(IParamsWSServer parameters, byte[] certificate, string certificatePassword)
        {
            _parameters = parameters;
            _certificate = certificate;
            _certificatePassword = certificatePassword;
        }

        public virtual void Start(CancellationToken cancellationToken)
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
                    _ = Task.Run(async () => { await ListenForConnectionsAsync(cancellationToken); });
                }
                else
                {
                    _ = Task.Run(async () => { await ListenForConnectionsSSLAsync(cancellationToken); });
                }
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerEventArgs
                {
                    Exception = ex,
                    Message = ex.Message,
                });
            }
        }
        public virtual void Stop()
        {
            _isRunning = false;

            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }

            _numberOfConnections = 0;

            FireEvent(this, new ServerEventArgs
            {
                ServerEventType = ServerEventType.Stop
            });
        }
 
        protected virtual async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync(cancellationToken);
                    var stream = client.GetStream();

                    var connection = new ConnectionWSServer
                    {
                        Websocket = WebSocket.CreateFromStream(stream, true, null, WebSocket.DefaultKeepAliveInterval),
                        ConnectionId = Guid.NewGuid().ToString(),
                        Stream = stream,
                        Client = client
                    };

                    _ = Task.Run(async () => { await StartReceivingMessagesAsync(connection, cancellationToken); });
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerEventArgs
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
                        var connection = new ConnectionWSServer
                        {
                            Websocket = WebSocket.CreateFromStream(sslStream, true, null, WebSocket.DefaultKeepAliveInterval),
                            ConnectionId = Guid.NewGuid().ToString(),
                            Client = client,
                            Stream = sslStream
                        };

                        _ = Task.Run(async () => { await StartReceivingMessagesAsync(connection, cancellationToken); }) ;
                    }
                    else
                    {
                        var certStatus = $"IsAuthenticated = {sslStream.IsAuthenticated} && IsEncripted == {sslStream.IsEncrypted}";
                        FireEvent(this, new WSErrorServerEventArgs
                        {
                            Exception = new Exception(certStatus),
                            Message = certStatus
                        });

                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    FireEvent(this, new WSErrorServerEventArgs
                    {
                        Exception = ex,
                        Message = ex.Message,
                    });
                }

            }
        }
        protected virtual async Task StartReceivingMessagesAsync(IConnectionWSServer connection, CancellationToken cancellationToken)
        {
            try
            {
                while (connection.Client.Connected)
                {
                    await Task.Delay(1);

                    if (connection.Client.Available < 3)
                    {
                        continue;
                    }; // match against "get"

                    var bytes = new byte[connection.Client.Available];
                    await connection.Stream.ReadAsync(bytes, 0, connection.Client.Available, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var data = Encoding.UTF8.GetString(bytes);

                        if (Regex.IsMatch(data, "^GET", RegexOptions.IgnoreCase))
                        {
                            if (await UpgradeConnectionAsync(data, connection, cancellationToken))
                            {
                                FireEvent(this, new WSConnectionServerEventArgs
                                {
                                    Connection = connection,
                                    ConnectionEventType = ConnectionEventType.Connected,
                                });
                            }
                        }
                        else
                        {
                            bool fin = (bytes[0] & 0b10000000) != 0,
                                mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                            int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                                msglen = bytes[1] - 128, // & 0111 1111
                                offset = 2;

                            if (msglen == 126)
                            {
                                // was ToUInt16(bytes, offset) but the result is incorrect
                                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                                offset = 4;
                            }
                            else if (msglen == 127)
                            {
                                // i don't really know the byte order, please edit this
                                // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                                // offset = 10;
                            }

                            if (msglen > 0 && mask)
                            {
                                var decoded = new byte[msglen];
                                var masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                                offset += 4;

                                for (int i = 0; i < msglen; ++i)
                                {
                                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);
                                }

                                var isDisconnect = false;
                                byte[] selectedBytes = null;

                                for (int i = 0; i < decoded.Length; i++)
                                {
                                    // This checks for a disconnect
                                    if (decoded[i] == 0x03)
                                    {
                                        isDisconnect = true;

                                        selectedBytes = new byte[i];
                                        for (int j = 0; j < i; j++)
                                        {
                                            selectedBytes[j] = decoded[j];
                                        }
                                        break;
                                    }
                                }

                                if (!isDisconnect)
                                {
                                    selectedBytes = decoded;
                                }

                                if (selectedBytes != null)
                                {
                                    var message = Encoding.UTF8.GetString(selectedBytes);

                                    if (!string.IsNullOrWhiteSpace(message))
                                    {
                                        if (message.Trim().ToLower() == "pong")
                                        {
                                            connection.HasBeenPinged = false;
                                        }
                                        else
                                        {
                                            MessageReceived(message, connection);
                                        }
                                    }

                                    if (isDisconnect)
                                    {
                                        await DisconnectConnectionAsync(connection);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            { }

            FireEvent(this, new WSConnectionServerEventArgs
            {
                Connection = connection,
                ConnectionEventType = ConnectionEventType.Disconnect,
            });
        }
        protected virtual async Task<bool> UpgradeConnectionAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken)
        {
            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            var swk = Regex.Match(message, $"{HttpKnownHeaderNames.SecWebSocketKey}: (.*)").Groups[1].Value.Trim();
            var swka = swk + Statics.WS_SERVER_GUID;
            var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            var swkaSha1Base64 = Convert.ToBase64String(swkaSha1);
            var requestedSubprotocols = Regex.Match(message, $"{HttpKnownHeaderNames.SecWebSocketProtocol}: (.*)").Groups[1].Value.Trim().Split(",");

            if (requestedSubprotocols.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray().Length > 0)
            {
                if (!AreSubprotocolsRequestedValid(requestedSubprotocols))
                {
                    var bytes = Encoding.UTF8.GetBytes("Invalid subprotocols requested");
                    await connection.Stream.WriteAsync(bytes);
                    await DisconnectConnectionAsync(connection);
                    return false;
                }
            }

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                $"{HttpKnownHeaderNames.Connection}: Upgrade\r\n" +
                $"{HttpKnownHeaderNames.Upgrade}: websocket\r\n" +
                $"{HttpKnownHeaderNames.SecWebSocketAccept}: " + swkaSha1Base64 + "\r\n\r\n");

            connection.SubProtocols = requestedSubprotocols;
            await connection.Stream.WriteAsync(response, 0, response.Length);

            _numberOfConnections++;

            await SendRawAsync(_parameters.ConnectionSuccessString, connection, cancellationToken);

            return true;
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
        protected virtual void MessageReceived(string message, IConnectionWSServer connection)
        {
            IPacket packet;

            try
            {
                packet = JsonConvert.DeserializeObject<Packet>(message);

                if (string.IsNullOrWhiteSpace(packet.Data))
                {
                    packet = new Packet
                    {
                        Data = message,
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch
            {
                packet = new Packet
                {
                    Data = message,
                    Timestamp = DateTime.UtcNow
                };
            }

            FireEvent(this, new WSMessageServerEventArgs
            {
                MessageEventType = MessageEventType.Receive,
                Packet = packet,
                Connection = connection
            });
        }
       
        public virtual async Task<bool> SendAsync<T>(T packet, IConnectionWSServer connection, CancellationToken cancellationToken) where T : IPacket
        {
            try
            {
                var message = JsonConvert.SerializeObject(packet);

                await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(message),
                    offset: 0,
                    count: message.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: cancellationToken);

                FireEvent(this, new WSMessageServerEventArgs
                {
                    MessageEventType = MessageEventType.Sent,
                    Packet = packet,
                    Connection = connection,
                });

                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerEventArgs
                {
                    Exception = ex,
                    Message = ex.Message,
                    Connection = connection,
                });

                await DisconnectConnectionAsync(connection);
            }

            return false;
        }
        public virtual async Task<bool> SendAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken)
        {
            var packet = new Packet
            {
                Data = message,
                Timestamp = DateTime.UtcNow
            };

            return await SendAsync(packet, connection, cancellationToken);
        }
        public virtual async Task<bool> SendRawAsync(string message, IConnectionWSServer connection, CancellationToken cancellationToken)
        {
            try
            {
                await connection.Websocket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(message),
                    offset: 0,
                    count: message.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: cancellationToken);

                FireEvent(this, new WSMessageServerEventArgs
                {
                    MessageEventType = MessageEventType.Sent,
                    Packet = new Packet
                    {
                        Data = message,
                        Timestamp = DateTime.UtcNow
                    },
                    Connection = connection,
                });

                return true;
            }
            catch (Exception ex)
            {
                _numberOfConnections--;
                FireEvent(this, new WSErrorServerEventArgs
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message,
                });
            }

            return false;
        }

        public virtual async Task<bool> DisconnectConnectionAsync(IConnectionWSServer connection)
        {
            try
            {
                _numberOfConnections--;

                if (connection.Websocket.State == WebSocketState.Open)
                {
                    await connection.Websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
                }

                FireEvent(this, new WSConnectionServerEventArgs
                {
                    Connection = connection,
                    ConnectionEventType = ConnectionEventType.Disconnect
                });
                return true;
            }
            catch (Exception ex)
            {
                FireEvent(this, new WSErrorServerEventArgs
                {
                    Connection = connection,
                    Exception = ex,
                    Message = ex.Message
                });
            }
            return false;
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

        public int NumberOfConnections
        {
            get
            {
                return _numberOfConnections;
            }
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