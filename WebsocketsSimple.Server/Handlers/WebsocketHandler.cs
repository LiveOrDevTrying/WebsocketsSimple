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
    public class WebsocketHandler : WebsocketHandlerBase<ConnectionWSServer>
    {
        public WebsocketHandler(ParamsWSServer parameters) : base(parameters)
        {
        }

        public WebsocketHandler(ParamsWSServer parameters, byte[] certificate, string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }

        protected override ConnectionWSServer CreateConnection(TcpClient client, Stream stream)
        {
            return new ConnectionWSServer
            {
                ConnectionId = Guid.NewGuid().ToString(),
                Stream = stream,
                Client = client
            };
        }
    }
}