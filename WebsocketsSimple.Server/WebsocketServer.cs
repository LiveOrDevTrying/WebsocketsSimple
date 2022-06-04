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
            WSConnectionManager, 
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

        protected override WSConnectionManager CreateConnectionManager()
        {
            return new WSConnectionManager();
        }

        protected override WebsocketHandler CreateHandler(byte[] certificate = null, string certificatePassword = null)
        {
            return certificate != null
                ? new WebsocketHandler(_parameters, certificate, certificatePassword)
                : new WebsocketHandler(_parameters);
        }

        protected override WSConnectionServerEventArgs CreateConnectionEventArgs(WSConnectionServerBaseEventArgs<ConnectionWSServer> args)
        {
            return new WSConnectionServerEventArgs
            {
                Connection = args.Connection,
                ConnectionEventType = args.ConnectionEventType
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

        protected override WSErrorServerEventArgs CreateErrorEventArgs(WSErrorServerBaseEventArgs<ConnectionWSServer> args)
        {
            return new WSErrorServerEventArgs
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message
            };
        }
    }
}
