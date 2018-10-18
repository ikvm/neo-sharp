using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoSharp.Core.Messaging;

namespace NeoSharp.Core.NewNetwork.Protocols
{
    public class ProtocolV2 : IProtocol
    {
        #region IProtocol Implementation 
        public uint Version =>  throw new System.NotImplementedException();

        public bool IsDefault => false;

        public Task<Message> ReceiveMessageAsync(Stream stream, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task SendMessageAsync(Stream stream, Message message, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}