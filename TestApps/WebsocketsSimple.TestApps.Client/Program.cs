using PHS.Networking.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebsocketsSimple.Client;
using WebsocketsSimple.Client.Events.Args;
using WebsocketsSimple.Client.Models;
using WebsocketsSimple.Core;

namespace WebsocketsSimple.TestApps.Client
{
    class Program
    {
        private static IWebsocketClient _client;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Push any key to start");

            Console.ReadLine();

            _client = new WebsocketClient(new ParamsWSClient
            {
                IsWebsocketSecured = false,
                Port = 65214,
                Uri = "localhost",
                RequestedSubProtocols = new string[] { "testProtocol", "test2", "test3" },
                RequestHeaders = new Dictionary<string, string> { { HttpKnownHeaderNames.From, "Robbie" } },
                KeepAliveInterval = TimeSpan.FromSeconds(5)
            }, token: "Test");
            _client.ConnectionEvent += OnConnectionEvent;
            _client.MessageEvent += OnMessageEvent;
            _client.ErrorEvent += OnErrorEvent;
            await _client.ConnectAsync();

            while (true)
            {
                await _client.SendToServerAsync(Console.ReadLine());
            }
        }

        private static Task OnErrorEvent(object sender, WSErrorClientEventArgs args)
        {
            Console.WriteLine(args.Message);
            return Task.CompletedTask;
        }

        private static Task OnMessageEvent(object sender, WSMessageClientEventArgs args)
        {
            switch (args.MessageEventType)
            {
                case MessageEventType.Sent:
                    break;
                case MessageEventType.Receive:
                    Console.WriteLine(args.Message);
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        private static Task OnConnectionEvent(object sender, WSConnectionClientEventArgs args)
        {
            Console.WriteLine(args.ConnectionEventType.ToString());
            return Task.CompletedTask;
        }
    }
}
