using PHS.Networking.Enums;
using PHS.Networking.Models;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Client.Models;
using WebsocketsSimple.Core;
using WebsocketsSimple.Core.Events.Args;
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client.Models
{
    public class WebsocketClientHandler : WebsocketClientHandlerBase<
        WSConnectionClientEventArgs,
        WSMessageClientEventArgs,
        WSErrorClientEventArgs,
        ParamsWSClient,
        ConnectionWS>
    {
        public WebsocketClientHandler(ParamsWSClient parameters) : base(parameters)
        {
        }

        protected override ConnectionWS CreateConnection(ConnectionWS connection)
        {
            return connection;
        }

        protected override WSConnectionClientEventArgs CreateConnectionEventArgs(WSConnectionEventArgs<ConnectionWS> args)
        {
            return new WSConnectionClientEventArgs
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType,
                CancellationToken = args.CancellationToken,
            };
        }

        protected override WSErrorClientEventArgs CreateErrorEventArgs(WSErrorEventArgs<ConnectionWS> args)
        {
            return new WSErrorClientEventArgs
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message,
                CancellationToken = args.CancellationToken,
            };
        }

        protected override WSMessageClientEventArgs CreateMessageEventArgs(WSMessageEventArgs<ConnectionWS> args)
        {
            return new WSMessageClientEventArgs
            {
                Bytes = args.Bytes,
                Connection = args.Connection,
                Message = args.Message,
                MessageEventType = args.MessageEventType,
                CancellationToken = args.CancellationToken,
            };
        }
    }
}
