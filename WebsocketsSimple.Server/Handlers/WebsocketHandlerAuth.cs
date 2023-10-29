using PHS.Networking.Events.Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Core;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public delegate void WebsocketAuthorizeEvent<T>(object sender, WSAuthorizeEventArgs<T> args);

    public class WebsocketHandlerAuth<T> :
        WebsocketHandlerAuthBase<
            WSConnectionServerAuthEventArgs<T>,
            WSMessageServerAuthEventArgs<T>,
            WSErrorServerAuthEventArgs<T>,
            ParamsWSServerAuth,
            WSAuthorizeEventArgs<T>,
            IdentityWSServer<T>,
            T>
    {
        public WebsocketHandlerAuth(ParamsWSServerAuth parameters) : base(parameters)
        {
        }
        public WebsocketHandlerAuth(ParamsWSServerAuth parameters, byte[] certificate, string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }

        protected override Task UpgradeConnectionAsync(string message, string[] requestedSubprotocols, Dictionary<string, string> requestHeaders, IdentityWSServer<T> connection, CancellationToken cancellationToken)
        {
            SetPathAndQueryStringForConnection(message, connection);

            string token = null;
            if (requestHeaders.ContainsKey(HttpKnownHeaderNames.Authorization))
            {
                var split = requestHeaders[HttpKnownHeaderNames.Authorization].Split(" ");
                if (split.Length == 2)
                {
                    token = split[1];
                }
            }
            else if (connection.QueryStringParameters.Any(x => x.Key.Trim().ToLower() == "token"))
            {
                token = connection.QueryStringParameters.FirstOrDefault(x => x.Key.Trim().ToLower() == "token").Value;
            };

            if (token != null)
            {
                FireEvent(this, new WSAuthorizeEventArgs<T>
                {
                    Connection = connection,
                    UpgradeData = message,
                    RequestSubprotocols = requestedSubprotocols,
                    Token = Encoding.UTF8.GetBytes(token),
                    RequestHeaders = requestHeaders,
                    CancellationToken = cancellationToken
                });
            }

            return Task.CompletedTask;
        }

        protected override IdentityWSServer<T> CreateConnection(ConnectionWSServer connection)
        {
            return new IdentityWSServer<T>
            {
                TcpClient = connection.TcpClient,
                ConnectionId = Guid.NewGuid().ToString(),
                SslStream = connection.SslStream,
                ReadBuffer = connection.ReadBuffer
            };
        }

        protected override WSMessageServerAuthEventArgs<T> CreateMessageEventArgs(WSMessageServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            return new WSMessageServerAuthEventArgs<T>
            {
                Bytes = args.Bytes,
                Connection = args.Connection,
                Message = args.Message,
                MessageEventType = args.MessageEventType,
                CancellationToken = args.CancellationToken
            };
        }

        protected override WSConnectionServerAuthEventArgs<T> CreateConnectionEventArgs(WSConnectionServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            return new WSConnectionServerAuthEventArgs<T>
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType,
                CancellationToken = args.CancellationToken,
                RequestHeaders = args.RequestHeaders
            };
        }

        protected override WSErrorServerAuthEventArgs<T> CreateErrorEventArgs(ErrorEventArgs<IdentityWSServer<T>> args)
        {
            return new WSErrorServerAuthEventArgs<T>
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message,
                CancellationToken = args.CancellationToken
            };
        }
    }
}