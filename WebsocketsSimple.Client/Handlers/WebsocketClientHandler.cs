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
using WebsocketsSimple.Core.Models;

namespace WebsocketsSimple.Client.Models
{
    public class WebsocketClientHandler : WebsocketClientHandlerBase<ConnectionWS>
    {
        public WebsocketClientHandler(ParamsWSClient parameters, string token = "") : base(parameters, token)
        {
        }

        protected override ConnectionWS CreateConnection(ConnectionWS connection)
        {
            return connection;
        }
    }
}
