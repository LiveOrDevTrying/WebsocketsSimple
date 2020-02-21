using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PHS.Networking.Enums;
using PHS.Tcp.Core.Async.Server.Models;
using WebsocketsSimple.Server;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Managers;

namespace WebsocketsSimple.TestApps.Server
{
    public class Startup
    {
        private IWebsocketServerAuth<Guid> _server;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            _server = new WebsocketServerAuth<Guid>(new ParamsWSServerAuth
            {
                ConnectionSuccessString = "Successfully connected to WebSocket",
                ConnectionUnauthorizedString = "Could not connect to WebSocket. Please check your OAuth token",
            }, new MockUserService());
            _server.MessageEvent += OnServerMessageEvent;
            services.AddSingleton(_server);

            //    _server = new WebsocketServer(new ParamsWSServer
            //    {
            //        ConnectionSuccessString = "Successfully connected to WebSocket",
            //    });
            //    _server.MessageEvent += OnServerMessageEvent;
            //    services.AddSingleton(_server);
            //}
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            app.MapWebSocketManager(serviceProvider.GetService<IWebsocketServerAuth<Guid>>());
        }

        protected virtual async Task OnServerMessageEvent(object sender, WSMessageServerEventArgs args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    break;
                case MessageEventType.Receive:
                    await _server.SendToConnectionAsync(args.Packet, args.Connection);
                    break;
                default:
                    break;
            }
        }
    }
}
