using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network;
using NeoSharp.Core.NewNetwork.Protocols;

namespace NeoSharp.Core.NewNetwork.Tcp
{
    public class TcpPeer : IPeer
    {
        #region Private Fields 
        private const int SocketOperationTimeout = 300_000;

        private readonly NetworkConfig _config;
        private readonly IServerContext _serverContext;
        private readonly IEnumerable<IProtocol> _protocols;
        private readonly IEnumerable<NewNetwork.Handlers.IMessageHandler> _messageHandlers;
        private readonly ILogger<TcpPeer> _logger;

        private CancellationTokenSource _cancelationTokenSource;
        private readonly bool _forceIPv6;
        private Socket _peerSocket;
        private NetworkStream _networkStream;
        private IProtocol _chosenProtocol;

        private IPAddress _peerIpAddress;
        private int _peerPort;

        private readonly SafeQueue<Message> _sendMessageQueue = new SafeQueue<Message>();
        #endregion

        #region Constructor 
        public TcpPeer(
            NetworkConfig config,
            IServerContext serverContext,
            IEnumerable<IProtocol> protocols,
            IEnumerable<NewNetwork.Handlers.IMessageHandler> messageHandlers, 
            ILogger<TcpPeer> logger)
        {   
            this._config = config;
            this._serverContext = serverContext;
            this._protocols = protocols;
            this._messageHandlers = messageHandlers;
            this._logger = logger;
            
            this._forceIPv6 = _config.ForceIPv6;
            this._chosenProtocol = protocols.Single(x => x.IsDefault);
        }
        #endregion

        #region IPeer Implementation
        public bool IsReady { get; set; }

        public VersionPayload Version { get; set; }

        public bool CanHandle(Protocol protocol)
        {
            return protocol == Protocol.Tcp;
        }

        public async void Connect(Network.EndPoint peerEndpoint, CancellationTokenSource cancellationTokenSource)
        {
            this._cancelationTokenSource = cancellationTokenSource;

            var peerIpAddress = await peerEndpoint.GetIPAddress();

            this._peerIpAddress = peerIpAddress.MapToIPv6OrIPv4(_forceIPv6);
            this._peerPort = peerEndpoint.Port;

            this._logger.LogInformation($"Connecting to {this._peerIpAddress}:{this._peerPort}...");
            this._peerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await this._peerSocket.ConnectAsync(this._peerIpAddress, this._peerPort);
                this._logger.LogInformation($"Connected to {this._peerIpAddress}:{this._peerPort}");

                this._networkStream = new NetworkStream(this._peerSocket, true);

                this.InitializeSendMessageQueueMonitor(cancellationTokenSource.Token);
                this.InitializePeerListener();
            }
            catch(Exception ex)
            {
                // TODO [AboimPinto]: What happen when the connection has not been established??? Previous code had no fail-safe measure
                this._logger.LogCritical(ex, $"Connection to {this._peerIpAddress}:{this._peerPort} as not extablished.");
            }
        }

        public void Disconnect()
        {
            this._cancelationTokenSource.Cancel();

            this._peerSocket.Shutdown(SocketShutdown.Both);
            this._networkStream.Dispose();
            this._peerSocket.Dispose();

            this._logger.LogInformation($"The peer {this._peerIpAddress}:{this._peerPort} was disconnected.");
        }

        public void QueueMessageToSend<TMessage>() where TMessage : Message, new()
        {
            this.QueueMessageToSend(new TMessage());
        }

        public void QueueMessageToSend(Message message)
        {
            this._sendMessageQueue.Enqueue(message);
        }
        #endregion

        #region Private Fields 
        private void InitializePeerListener()
        {
            // Initiate handshake
            this.QueueMessageToSend(new VersionMessage(this._serverContext.Version));

            Task.Factory.StartNew(async () => 
            {
                while(!this._cancelationTokenSource.IsCancellationRequested)
                {
                    if (this._networkStream.DataAvailable)
                    {
                        var message = await this.Receive();
                        await this.HandleMessage(message);
                    }
                }
            }, this._cancelationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }        

        private void InitializeSendMessageQueueMonitor(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    this._sendMessageQueue.WaitForQueueToChange();

                    var message = this._sendMessageQueue.Dequeue();

                    if (message == null) continue;          // When the last message, the list is changed but there is not message to dequeue

                    if (message.Command != MessageCommand.consensus)
                    {
                        await this.SendMessage(message);
                    }
                }
            });
        }

        private async Task SendMessage(Message message)
        {
            using(var socketTokenSource = new CancellationTokenSource(SocketOperationTimeout))
            {
                socketTokenSource.Token.Register(this.Disconnect);

                try
                {
                    this._logger.LogInformation($"Message {message.Command} send to {this._peerIpAddress}:{this._peerPort}");
                    await this._chosenProtocol.SendMessageAsync(this._networkStream, message, socketTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while send message {message.Command} to {this._peerIpAddress}:{this._peerPort}.");
                    this.Disconnect();
                }
            }
        }

        public async Task<Message> Receive()
        {
            using (var tokenSource = new CancellationTokenSource(SocketOperationTimeout))
            {
                tokenSource.Token.Register(Disconnect);

                try
                {
                    var msg = await this._chosenProtocol.ReceiveMessageAsync(this._networkStream, tokenSource.Token);
                    this._logger.LogDebug($"Message Received: {msg.Command}");
                    return msg;
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error while receive");
                    this.Disconnect();
                }
            }
    
            return null;
        }

        private async Task HandleMessage(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var messageHandler = this._messageHandlers.SingleOrDefault(x => x.CanHandle(message));

            if (messageHandler == null)
            {
                this._logger.LogError($"could not find the handler for the message type {message.GetType()}");
                return;
            }

            var startedAt = DateTime.UtcNow;
            _logger.LogDebug($"The message handler \"{messageHandler.GetType().Name}\" started message handling at {startedAt:yyyy-MM-dd HH:mm:ss}.");

            var handler = this._messageHandlers.Single(x => x.CanHandle(message));
            await handler.Handle(message, this);
            var completedAt = DateTime.UtcNow;

            var handledWithin = (completedAt - startedAt).TotalSeconds;

            _logger.LogDebug(
                $"The message handler \"{messageHandler.GetType().Name}\" completed message handling at {completedAt:yyyy-MM-dd HH:mm:ss} ({handledWithin} s).");
        }
        #endregion
    }
}