using PHS.Networking.Enums;
using PHS.Networking.Events;
using PHS.Networking.Models;
using PHS.Networking.Server.Enums;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Server.Services;
using PHS.Networking.Services;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Managers;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server
{
    public class WebsocketServerAuth<T> :
        WebsocketServerAuthBase<
            WSConnectionServerAuthEventArgs<T>, 
            WSMessageServerAuthEventArgs<T>, 
            WSErrorServerAuthEventArgs<T>, 
            ParamsWSServerAuth,
            WebsocketHandlerAuth<T>, 
            WSConnectionManagerAuth<T>,
            IdentityWSServer<T>,
            T,
            WSAuthorizeEventArgs<T>>, 
        IWebsocketServerAuth<T>
    {
        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService) : base(parameters, userService)
        {
        }

        public WebsocketServerAuth(ParamsWSServerAuth parameters,
            IUserService<T> userService,
            byte[] certificate,
            string certificatePassword) : base(parameters, userService, certificate, certificatePassword)
        {
        }

        protected override WebsocketHandlerAuth<T> CreateHandler()
        {
            return _certificate != null
                ? new WebsocketHandlerAuth<T>(_parameters, _certificate, _certificatePassword)
                : new WebsocketHandlerAuth<T>(_parameters);
        }
        protected override WSConnectionManagerAuth<T> CreateConnectionManager()
        {
            return new WSConnectionManagerAuth<T>();
        }

        protected override WSErrorServerAuthEventArgs<T> CreateErrorEventArgs(WSErrorServerBaseEventArgs<IdentityWSServer<T>> args)
        {
            return new WSErrorServerAuthEventArgs<T>
            {
                Connection = args.Connection,
                Exception = args.Exception,
                Message = args.Message,
                CancellationToken = args.CancellationToken,
            };
        }
    }
}
