using PHS.Networking.Server.Events.Args;
using System;
using System.Threading.Tasks;
using PHS.Networking.Enums;
using WebsocketsSimple.Server;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Events.Args;

namespace WebsocketsSimple.TestApps.Server
{
    class Program
    {
        private static WebsocketServerAuth<Guid> _authServer;
        private static WebsocketServer _server;

        static void Main(string[] args)
        {
            //_server = new WebsocketServer(new ParamsWSServer
            //{
            //    ConnectionSuccessString = "Connected Successfully",
            //    Port = 65214,
            //    AvailableSubprotocols = new string[] { "testProtocol", "test2", "test3", "another" }
            //});
            //_server.MessageEvent += OnMessageEventReg; ;
            //_server.ServerEvent += OnServerEvent;
            //_server.ConnectionEvent += OnConnectionEventReg;
            //_server.ErrorEvent += OnErrorEventReg;
            //_server.Start();

            _authServer = new WebsocketServerAuth<Guid>(new ParamsWSServer
            {
                ConnectionSuccessString = "Connected Successfully",
                Port = 65214,
                AvailableSubprotocols = new string[] { "testProtocol", "test2", "test3", "another" }
            }, new MockUserService());
            _authServer.MessageEvent += OnMessageEvent;
            _authServer.ServerEvent += OnServerEvent;
            _authServer.ConnectionEvent += OnConnectionEvent;
            _authServer.ErrorEvent += OnErrorEvent;
            _authServer.Start();

            while (true)
            {
                Console.ReadLine();
            }
        }

        //private static void OnErrorEventReg(object sender, WSErrorServerEventArgs args)
        //{
        //    Console.WriteLine(args.Message);
        //}

        //private static void OnConnectionEventReg(object sender, WSConnectionServerEventArgs args)
        //{
        //    Console.WriteLine(args.ConnectionEventType + " " + _authServer.ConnectionCount);
        //}

        //private static void OnMessageEventReg(object sender, WSMessageServerEventArgs args)
        //{
        //    switch (args.MessageEventType)
        //    {
        //        case MessageEventType.Sent:
        //            break;
        //        case MessageEventType.Receive:
        //            Console.WriteLine(args.MessageEventType + ": " + args.Message);

        //            Task.Run(async () =>
        //            {
        //                await _server.BroadcastToAllConnectionsAsync(args.Message, args.Connection);

        //            });
        //            break;
        //        default:
        //            break;
        //    }
        //}

        private static void OnErrorEvent(object sender, WSErrorServerEventArgs<IdentityWSServer<Guid>> args)
        {
            Console.WriteLine(args.Message);
        }

        private static void OnConnectionEvent(object sender, WSConnectionServerEventArgs<IdentityWSServer<Guid>> args)
        {
            Console.WriteLine(args.ConnectionEventType + " " + _authServer.ConnectionCount);
        }

        private static void OnServerEvent(object sender, ServerEventArgs args)
        {
            Console.WriteLine(args.ServerEventType);
        }

        private static void OnMessageEvent(object sender, WSMessageServerEventArgs<IdentityWSServer<Guid>> args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    break;
                case MessageEventType.Receive:
                    Console.WriteLine(args.MessageEventType + ": " + args.Message);


                    Task.Run(async () =>
                    {
                        Console.WriteLine("Connections: " + _authServer.ConnectionCount);
                        await _authServer.BroadcastToAllConnectionsAsync(args.Message, args.Connection);
                    });
                    break;
                default:
                    break;
            }
        }
    }
}
