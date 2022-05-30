using PHS.Networking.Enums;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Handlers;

namespace WebsocketsSimple.Server
{
    public class WebsocketServer :
        WebsocketServerBase<
            WSConnectionServerEventArgs,
            WSMessageServerEventArgs,
            WSErrorServerEventArgs, 
            ParamsWSServer, 
            WebsocketHandler, 
            WSConnectionManager<ConnectionWSServer>, 
            ConnectionWSServer>,
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

        protected override WSConnectionManager<ConnectionWSServer> CreateConnectionManager()
        {
            return new WSConnectionManager<ConnectionWSServer>();
        }
        protected override WebsocketHandler CreateHandler(byte[] certificate = null, string certificatePassword = null)
        {
            return certificate != null
                ? new WebsocketHandler(_parameters, certificate, certificatePassword)
                : new WebsocketHandler(_parameters);
        }

        protected override void OnConnectionEvent(object sender, WSConnectionServerEventArgs args)
        {
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    _connectionManager.Add(args.Connection.ConnectionId, args.Connection);
                    break;
                case ConnectionEventType.Disconnect:
                    _connectionManager.Remove(args.Connection.ConnectionId);
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
            FireEvent(this, args);
        }
    }
}
