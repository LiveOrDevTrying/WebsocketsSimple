using PHS.Networking.Enums;
using PHS.Networking.Models;
using PHS.Networking.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace WebsocketsSimple.Client
{
    public class WebsocketClient :
        WebsocketClientBase<
            WSConnectionClientEventArgs, 
            WSMessageClientEventArgs, 
            WSErrorClientEventArgs, 
            ParamsWSClient, 
            WebsocketClientHandler, 
            ConnectionWS>,
        IWebsocketClient
    {
        public WebsocketClient(ParamsWSClient parameters) : base(parameters)
        {
        }

        protected override void OnConnectionEvent(object sender, WSConnectionEventArgs<ConnectionWS> args)
        {
            FireEvent(this, new WSConnectionClientEventArgs
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType
            });
        }
        protected override void OnMessageEvent(object sender, WSMessageEventArgs<ConnectionWS> args)
        {
            FireEvent(this, new WSMessageClientEventArgs
            {
                Bytes = args.Bytes,
                Connection = args.Connection,
                Message = args.Message,
                MessageEventType = args.MessageEventType
            });
        }
        protected override void OnErrorEvent(object sender, WSErrorEventArgs<ConnectionWS> args)
        {
            FireEvent(this, new WSErrorClientEventArgs
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message
            });
        }

        protected override WebsocketClientHandler CreateWebsocketClientHandler()
        {
            return new WebsocketClientHandler(_parameters);
        }
    }
}
