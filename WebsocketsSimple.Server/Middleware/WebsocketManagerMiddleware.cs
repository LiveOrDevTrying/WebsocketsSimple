using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using WebsocketsSimple.Server.Models;

namespace WebsocketsSimple.Server.Middleware
{
    public class WebsocketManagerMiddleware
    {
        protected readonly RequestDelegate _next;
        protected readonly IWebsocketServer _websocketServer;

        public WebsocketManagerMiddleware(RequestDelegate next,
            IWebsocketServer websocketServer)
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
            var websocket = await context.WebSockets.AcceptWebSocketAsync();

            var connection = new ConnectionServer
            {
                Websocket = websocket,
                ConnectionId = Guid.NewGuid().ToString()
            };

            await _websocketServer.StartReceivingAsync(connection);
            
            await _next.Invoke(context);
        }
    }
}
