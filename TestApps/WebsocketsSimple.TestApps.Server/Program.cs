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

        static void Main(string[] args)
        {
            _authServer = new WebsocketServerAuth<Guid>(new ParamsWSServerAuth
            {
                ConnectionSuccessString = "Connected Successfully",
                Port = 65214,
                ConnectionUnauthorizedString = "Not authorized",
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

        private static void OnErrorEvent(object sender, WSErrorServerAuthEventArgs<Guid> args)
        {
            Console.WriteLine(args.Message);
        }

        private static void OnConnectionEvent(object sender, WSConnectionServerAuthEventArgs<Guid> args)
        {
            Console.WriteLine(args.ConnectionEventType);
        }

        private static void OnServerEvent(object sender, ServerEventArgs args)
        {
            Console.WriteLine(args.ServerEventType);
        }

        private static void OnMessageEvent(object sender, WSMessageServerAuthEventArgs<Guid> args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    break;
                case MessageEventType.Receive:
                    Console.WriteLine(args.MessageEventType + ": " + args.Message);

                    Task.Run(async () =>
                    {
                        await _authServer.SendToConnectionAsync(args.Message, args.Connection);

                    });
                    break;
                default:
                    break;
            }
        }
    }
}
