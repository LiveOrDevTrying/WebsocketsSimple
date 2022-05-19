using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Models;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using PHS.Networking.Events;
using PHS.Networking.Server.Enums;
using PHS.Networking.Enums;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;

namespace WebsocketsSimple.Server
{
    public class WebsocketServer : 
        WebsocketServerBase<WSConnectionServerEventArgs, WSMessageServerEventArgs, WSErrorServerEventArgs, ParamsWSServer, WebsocketHandler, WSConnectionManager>, 
        IWebsocketServer
    {
        public WebsocketServer(ParamsWSServer parameters) : base(parameters)
        {
        }
        public WebsocketServer(ParamsWSServer parameters,
            byte[] certificate,
            string certificatePassword) : base(parameters, certificate, certificatePassword)
        {
        }

        protected override WebsocketHandler CreateWebsocketHandler(byte[] certificate = null, string certificatePassword = null)
        {
            if (certificate == null)
            {
                return new WebsocketHandler(_parameters);
            }
            else
            {
                return new WebsocketHandler(_parameters, certificate, certificatePassword);
            }
        }
        protected override WSConnectionManager CreateWSConnectionManager()
        {
            return new WSConnectionManager();
        }

        protected override void OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    _connectionManager.AddConnection(args.Connection);
                    break;
                case ConnectionEventType.Disconnect:
                    _connectionManager.RemoveConnection(args.Connection);
                    break;
                case ConnectionEventType.Connecting:
                    break;
                default:
                    break;
            }

            FireEvent(this, args);
        }
        protected override void OnErrorEvent(object sender, WSErrorServerEventArgs args)
        {
            FireEvent(this, args);
        }
        protected override void OnMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            FireEvent(sender, args);
        }
    }
}
