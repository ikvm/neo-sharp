using System.Threading;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network;

namespace NeoSharp.Core.NewNetwork
{
    public interface IPeer
    {
        bool IsReady { get; set; }

        VersionPayload Version { get; set; }

        bool CanHandle(Protocol protocol);

        void Connect(EndPoint peerEndpoint, CancellationTokenSource cancellationTokenSource);

         void Disconnect();

        void QueueMessageToSend<TMessage>() where TMessage : Message, new();

         void QueueMessageToSend(Message message);
    }
}