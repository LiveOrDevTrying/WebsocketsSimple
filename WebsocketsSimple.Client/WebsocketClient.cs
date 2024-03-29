﻿using PHS.Networking.Enums;
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
        CoreNetworkingClient<
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

        protected override WebsocketClientHandler CreateHandler()
        {
            return new WebsocketClientHandler(_parameters);
        }

        public virtual async Task<bool> DisconnectAsync(WebSocketCloseStatus webSocketCloseStatus = WebSocketCloseStatus.NormalClosure,
           string closeStatusDescription = "Disconnect",
           CancellationToken cancellationToken = default)
        {
            return await _handler.DisconnectAsync(webSocketCloseStatus, closeStatusDescription, cancellationToken).ConfigureAwait(false);
        }
    }
}
