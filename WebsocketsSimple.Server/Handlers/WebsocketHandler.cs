﻿using PHS.Networking.Events.Args;
using PHS.Networking.Models;
using System;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Handlers
{
    public class WebsocketHandler : 
        WebsocketHandlerBase<
            WSConnectionServerEventArgs,
            WSMessageServerEventArgs,
            WSErrorServerEventArgs,
            ParamsWSServer,
            ConnectionWSServer>
    {
        public WebsocketHandler(ParamsWSServer parameters) : base(parameters)
        {
        }

        public WebsocketHandler(ParamsWSServer parameters, byte[] certificate, string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }

        protected override ConnectionWSServer CreateConnection(ConnectionTcpClient connection)
        {
            return new ConnectionWSServer
            {
                ConnectionId = Guid.NewGuid().ToString(),
                TcpClient = connection.TcpClient
            };
        }

        protected override WSConnectionServerEventArgs CreateConnectionEventArgs(ConnectionEventArgs<ConnectionWSServer> args)
        {
            return new WSConnectionServerEventArgs
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType
            };
        }

        protected override WSErrorServerEventArgs CreateErrorEventArgs(ErrorEventArgs<ConnectionWSServer> args)
        {
            return new WSErrorServerEventArgs
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message
            };
        }

        protected override WSMessageServerEventArgs CreateMessageEventArgs(WSMessageServerBaseEventArgs<ConnectionWSServer> args)
        {
            return new WSMessageServerEventArgs
            {
                Bytes = args.Bytes,
                Connection = args.Connection,
                Message = args.Message,
                MessageEventType = args.MessageEventType
            };
        }
    }
}