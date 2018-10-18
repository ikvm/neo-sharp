using System.Threading.Tasks;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;

namespace NeoSharp.Core.NewNetwork.Handlers
{
    public class InventoryMessageHandler : IMessageHandler
    {
        #region IMessageHandler implementation 
        public bool CanHandle(Message message)
        {
            return message is InventoryMessage;
        }

        public Task Handle(Message message, IPeer sourcePeer)
        {
            return Task.CompletedTask;
        }
        #endregion
    }
}