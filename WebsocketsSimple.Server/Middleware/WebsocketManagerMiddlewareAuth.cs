using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Middleware
{
    public class WebsocketManagerMiddlewareAuth<T>
    {
        protected readonly RequestDelegate _next;
        protected readonly IWebsocketServerAuth<T> _websocketServer;

        public WebsocketManagerMiddlewareAuth(RequestDelegate next,
            IWebsocketServerAuth<T> websocketServer)
        {
            _next = next;
            _websocketServer = websocketServer;
        }

        public virtual async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return;
            }

            var token = context.Request.Path.ToString().Substring(1);
            var websocket = await context.WebSockets.AcceptWebSocketAsync();

            var connection = new ConnectionServer
            {
                Websocket = websocket,
                ConnectionId = Guid.NewGuid().ToString()
            };

            await _websocketServer.AuthorizeAndStartReceiving(connection, token);
            
            await _next.Invoke(context);
        }
    }
}
