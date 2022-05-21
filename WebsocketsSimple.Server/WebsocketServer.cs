using PHS.Networking.Enums;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Handlers;

namespace WebsocketsSimple.Server
{
    public class WebsocketServer :
        WebsocketServerBase<
            WSConnectionServerEventArgs<ConnectionWSServer>,
            WSMessageServerEventArgs<ConnectionWSServer>,
            WSErrorServerEventArgs<ConnectionWSServer>, 
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

        protected override WSConnectionManager<ConnectionWSServer> CreateWSConnectionManager()
        {
            return new WSConnectionManager<ConnectionWSServer>();
        }
        protected override WebsocketHandler CreateWebsocketHandler(byte[] certificate = null, string certificatePassword = null)
        {
            return certificate != null
                ? new WebsocketHandler(_parameters, certificate, certificatePassword)
                : new WebsocketHandler(_parameters);
        }

        protected override void OnConnectionEvent(object sender, WSConnectionServerEventArgs<ConnectionWSServer> args)
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

            FireEvent(this, new WSConnectionServerEventArgs<ConnectionWSServer>
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType
            });
        }
        protected override void OnErrorEvent(object sender, WSErrorServerEventArgs<ConnectionWSServer> args)
        {
            FireEvent(this, new WSErrorServerEventArgs<ConnectionWSServer> 
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message
            });
        }
        protected override void OnMessageEvent(object sender, WSMessageServerEventArgs<ConnectionWSServer> args)
        {
            FireEvent(sender, new WSMessageServerEventArgs<ConnectionWSServer>
            {
                Bytes = args.Bytes,
                Connection = args.Connection,
                Message = args.Message,
                MessageEventType = args.MessageEventType
            });
        }
    }
}
