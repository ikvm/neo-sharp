using System;
using System.Threading.Tasks;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network;

namespace NeoSharp.Core.NewNetwork.Handlers
{
    public class VersionMessageHandler : IMessageHandler
    {
        #region Private Fields 
        private readonly ILogger<VersionMessageHandler> _logger;
        private readonly IServerContext _serverContext;
        #endregion

        #region Constructor 
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverContext">Server</param>
        /// <param name="logger">Logger</param>
        public VersionMessageHandler(IServerContext serverContext, ILogger<VersionMessageHandler> logger)
        {
            _serverContext = serverContext;
            _logger = logger;
        }
        #endregion

        #region MessageHandler override methods
        /// <inheritdoc />
        public Task Handle(Message message, NewNetwork.IPeer sourcePeer)
        {
            var versionMessage = (VersionMessage)message;

            sourcePeer.Version = versionMessage.Payload;
            if (_serverContext.Version.Nonce == sourcePeer.Version.Nonce)
            {
                throw new InvalidOperationException($"The handshake is failed due to \"{nameof(_serverContext.Version.Nonce)}\" value equality.");
            }

            // TODO [AboimPinto]: this logic will be added later
            // Change protocol?
            // if (sender.ChangeProtocol(message.Payload))
            // {
            //     _logger?.LogWarning("Changed protocol.");
            // }

            // Send Ack
            sourcePeer.QueueMessageToSend<VerAckMessage>();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public bool CanHandle(Message message)
        {
            return message is VersionMessage;
        }
        #endregion
    }
}