using PHS.Networking.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebsocketsSimple.Client;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Client.Models;
using WebsocketsSimple.Core;

namespace WebsocketsSimple.TestApps.Client
{
    class Program
    {
        private static List<IWebsocketClient> _clients = new List<IWebsocketClient>();
        private static Timer _timer;

        static int CalculateNumberOfUsersPerMinute(int numberUsers)
        {
            return 60000 / numberUsers;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Enter numbers of users per minute:");
            var line = Console.ReadLine();
            var numberUsers = 0;
            while (!int.TryParse(line, out numberUsers))
            {
                Console.WriteLine("Invalid. Input an int:");
                line = Console.ReadLine();
            }

            Console.WriteLine("Push any key to start");

            Console.ReadLine();

            _timer = new Timer(OnTimerTick, null, 0, CalculateNumberOfUsersPerMinute(numberUsers));

            while(true)
            {
                line = Console.ReadLine();

                Task.Run(async () => await _clients.Where(x => x.IsRunning).OrderBy(x => Guid.NewGuid()).First().SendToServerAsync(line));
            }
        }

        private static void OnTimerTick(object state)
        {
            var client = new WebsocketClient(new ParamsWSClient
            {
                IsWebsocketSecured = false,
                Port = 65214,
                Host = "localhost",
                Path = "newPath",
                QueryStringParameters = new KeyValuePair<string, string>[]
                {
            new KeyValuePair<string, string>("TestQSParam", "TestQSValue")
                },
                RequestedSubProtocols = new string[] { "testProtocol", "test2", "test3" },
                RequestHeaders = new Dictionary<string, string> { { HttpKnownHeaderNames.From, "Robbie" } },
                KeepAliveInterval = TimeSpan.FromSeconds(5)
            }, "testToken");
            client.ConnectionEvent += OnConnectionEvent;
            client.MessageEvent += OnMessageEvent;
            client.ErrorEvent += OnErrorEvent;
            _clients.Add(client);
            
            Task.Run(async () => await client.ConnectAsync());
        }

        private static void OnErrorEvent(object sender, WSErrorClientEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        private static void OnMessageEvent(object sender, WSMessageClientEventArgs args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    break;
                case MessageEventType.Receive:
                    Console.WriteLine(args.Message + " : " + _clients.Where(x => x != null && x.IsRunning).Count());
                    break;
                default:
                    break;
            }
        }

        private static void OnConnectionEvent(object sender, WSConnectionClientEventArgs args)
        {
        }
    }
}
