using System.Threading.Tasks;
using NeoSharp.Core.Messaging;

namespace NeoSharp.Core.NewNetwork.Handlers
{
    public interface IMessageHandler
    {
         bool CanHandle(Message message);

         Task Handle(Message message, NewNetwork.IPeer sourcePeer);
    }
}