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
        public WebsocketClient(ParamsWSClient parameters, string token = "") : base(parameters, token)
        {
        }

        protected override void OnConnectionEvent(object sender, WSConnectionClientEventArgs args)
        {
            FireEvent(this, args);
        }
        protected override void OnMessageEvent(object sender, WSMessageClientEventArgs args)
        {
            FireEvent(this, args);
        }
        protected override void OnErrorEvent(object sender, WSErrorClientEventArgs args)
        {
            FireEvent(this, args);
        }

        protected override WebsocketClientHandler CreateWebsocketClientHandler()
        {
            return new WebsocketClientHandler(_parameters, _token);
        }
    }
}
