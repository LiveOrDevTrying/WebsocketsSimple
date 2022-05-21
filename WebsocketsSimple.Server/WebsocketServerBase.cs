﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PHS.Networking.Server.Events.Args;
using PHS.Networking.Services;
using PHS.Networking.Events;
using PHS.Networking.Server.Enums;
using WebsocketsSimple.Server.Events.Args;
using WebsocketsSimple.Server.Handlers;
using WebsocketsSimple.Server.Models;
using WebsocketsSimple.Server.Managers;
using System.Collections.Generic;
using WebsocketsSimple.Core.Events.Args;

namespace WebsocketsSimple.Server
{
    public abstract class WebsocketServerBase<T, U, V, W, X, Y, Z> : 
        CoreNetworking<T, U, V>,
        ICoreNetworking<T, U, V>
        where T : WSConnectionEventArgs<Z>
        where U : WSMessageEventArgs<Z>
        where V : WSErrorEventArgs<Z>
        where W : ParamsWSServer
        where X : WebsocketHandlerBase<Z>
        where Y : WSConnectionManager<Z>
        where Z : ConnectionWSServer
    {
        protected readonly X _handler;
        protected readonly W _parameters;
        protected readonly Y _connectionManager;
        protected Timer _timerPing;
        protected volatile bool _isPingRunning;
        protected CancellationToken _cancellationToken;

        protected event NetworkingEventHandler<ServerEventArgs> _serverEvent;

        public WebsocketServerBase(W parameters)
        {
            _parameters = parameters;
            _connectionManager = CreateWSConnectionManager();

            _handler = CreateWebsocketHandler();
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
        }
        public WebsocketServerBase(W parameters,
            byte[] certificate,
            string certificatePassword)
        {
            _parameters = parameters;
            _connectionManager = CreateWSConnectionManager();

            _handler = CreateWebsocketHandler(certificate, certificatePassword);
            _handler.ConnectionEvent += OnConnectionEvent;
            _handler.MessageEvent += OnMessageEvent;
            _handler.ErrorEvent += OnErrorEvent;
            _handler.ServerEvent += OnServerEvent;
        }
 
        public virtual void Start(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _handler.Start(cancellationToken);
        }
        public virtual void Stop()
        {
            _handler.Stop();
        }

        public virtual async Task<bool> BroadcastToAllConnectionsAsync(string message, Z connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll())
                {
                    if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
                    {
                        await SendToConnectionAsync(message, connection, cancellationToken);
                    }
                }

                return true;
            }

            return false;
        }
        public virtual async Task<bool> BroadcastToAllConnectionsAsync(byte[] message, Z connectionSending = null, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                foreach (var connection in _connectionManager.GetAll())
                {
                    if (connectionSending == null || connection.ConnectionId != connectionSending.ConnectionId)
                    {
                        await SendToConnectionAsync(message, connection, cancellationToken);
                    }
                }

                return true;
            }

            return false;
        }

        public virtual async Task<bool> SendToConnectionAsync(string message, Z connection, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                return await _handler.SendAsync(message, connection, cancellationToken);
            }
            
            return false;
        }
        public virtual async Task<bool> SendToConnectionAsync(byte[] message, Z connection, CancellationToken cancellationToken = default)
        {
            if (IsServerRunning)
            {
                return await _handler.SendAsync(message, connection, cancellationToken);
            }
            
            return false;
        }

        public virtual async Task DisconnectConnectionAsync(Z connection, CancellationToken cancellationToken = default)
        {
            await _handler.DisconnectConnectionAsync(connection, cancellationToken);
        }

        protected abstract void OnConnectionEvent(object sender, WSConnectionServerEventArgs<Z> args);
        protected virtual void OnServerEvent(object sender, ServerEventArgs args)
        {
            switch (args.ServerEventType)
            {
                case ServerEventType.Start:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }

                    _timerPing = new Timer(OnTimerPingTick, null, _parameters.PingIntervalSec * 1000, _parameters.PingIntervalSec * 1000);
                    break;
                case ServerEventType.Stop:
                    if (_timerPing != null)
                    {
                        _timerPing.Dispose();
                        _timerPing = null;
                    }
                    break;
                default:
                    break;
            }

            FireEvent(sender, args);
        }
        protected abstract void OnMessageEvent(object sender, WSMessageServerEventArgs<Z> args);
        protected abstract void OnErrorEvent(object sender, WSErrorServerEventArgs<Z> args);

        protected virtual void OnTimerPingTick(object state)
        {
            if (!_isPingRunning)
            {
                _isPingRunning = true;

                Task.Run(async () =>
                {
                    var ts = DateTime.UtcNow;
                    foreach (var connection in _connectionManager.GetPingedConnections())
                    {
                        // Already been pinged, no response, disconnect
                        await SendToConnectionAsync("No ping response - disconnected.", connection, _cancellationToken);

                        await DisconnectConnectionAsync(connection, _cancellationToken);
                    }

                    foreach (var connection in _connectionManager.GetPingableConnections(_parameters.MaxConnectionsPingedPerInterval))
                    {
                        connection.HasBeenPinged = true;
                        connection.NextPingTime = ts + TimeSpan.FromSeconds(_parameters.PingIntervalSec); 
                        await SendToConnectionAsync("Ping", connection, _cancellationToken);
                    }

                    _isPingRunning = false;
                });
            }
        }

        protected abstract X CreateWebsocketHandler(byte[] certificate = null, string certificatePassword = null);
        protected abstract Y CreateWSConnectionManager();

        protected virtual void FireEvent(object sender, ServerEventArgs args)
        {
            _serverEvent?.Invoke(sender, args);
        }

        public override void Dispose()
        {
            foreach (var connection in _connectionManager.GetAll())
            {
                DisconnectConnectionAsync(connection).Wait();
            }

            if (_handler != null)
            {
                _handler.ConnectionEvent -= OnConnectionEvent;
                _handler.MessageEvent -= OnMessageEvent;
                _handler.ErrorEvent -= OnErrorEvent;
                _handler.ServerEvent -= OnServerEvent;
                _handler.Dispose();
            }

            if (_timerPing != null)
            {
                _timerPing.Dispose();
                _timerPing = null;
            }
            base.Dispose();
        }

        public TcpListener Server
        {
            get
            {
                return _handler?.Server;
            }
        }
        public bool IsServerRunning
        {
            get
            {
                return _handler != null && _handler.IsServerRunning;
            }
        }
        public IEnumerable<Z> Connections
        {
            get
            {
                return _connectionManager.GetAll();
            }
        }
        public int ConnectionCount
        {
            get
            {
                return _connectionManager.Count();
            }
        }

        public event NetworkingEventHandler<ServerEventArgs> ServerEvent
        {
            add
            {
                _serverEvent += value;
            }
            remove
            {
                _serverEvent -= value;
            }
        }
    }
}
