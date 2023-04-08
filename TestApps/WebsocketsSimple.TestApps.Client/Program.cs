﻿using PHS.Networking.Enums;
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
        private static int _max;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter numbers of users per minute:");
            var line = Console.ReadLine();
            var numberUsers = 0;
            while (!int.TryParse(line, out numberUsers))
            {
                Console.WriteLine("Invalid. Input an int:");
                line = Console.ReadLine();
            }

            Console.WriteLine("Enter max number of users:");
            line = Console.ReadLine();
            _max = 0;
            while (!int.TryParse(line, out _max))
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

                if (line == "restart")
                {
                    var clients = _clients.ToList();
                    _clients = new List<IWebsocketClient>();
                    foreach (var item in clients)
                    {
                        await item.DisconnectAsync();
                    }
                }
                else
                {
                      await _clients.ToList().Where(x => x.IsRunning).OrderBy(x => Guid.NewGuid()).First().SendAsync(line);
                }
            }
        }

        private static void OnTimerTick(object state)
        {
            if (_clients.Count < _max)
            {
                var client = new WebsocketClient(new ParamsWSClient("localhost", 65214, false, "testToken", "newPath", new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("TestQSParam", "TestQSValue")
                }, null, new string[] { "testProtocol", "test2", "test3" }));
               
                client.ConnectionEvent += OnConnectionEvent;
                client.MessageEvent += OnMessageEvent;
                client.ErrorEvent += OnErrorEvent;
                _clients.Add(client);

                Task.Run(async () => await client.ConnectAsync());
            }
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
            switch (args.ConnectionEventType)
            {
                case ConnectionEventType.Connected:
                    break;
                case ConnectionEventType.Disconnect:
                    var client = (IWebsocketClient)sender;
                    _clients.Remove(client);
                    
                    client.ConnectionEvent -= OnConnectionEvent;
                    client.MessageEvent -= OnMessageEvent;
                    client.ErrorEvent -= OnErrorEvent;

                    client.Dispose();
                    break;
                default:
                    break;
            }
        }

        static int CalculateNumberOfUsersPerMinute(int numberUsers)
        {
            return 60000 / numberUsers;
        }
    }
}
