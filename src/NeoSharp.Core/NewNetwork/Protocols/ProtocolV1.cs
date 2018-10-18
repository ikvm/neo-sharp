using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network;

namespace NeoSharp.Core.NewNetwork.Protocols
{
    public class ProtocolV1 : IProtocol
    {
        #region Private Fields 
        private readonly uint _magic;
        private readonly IBinarySerializer _binarySerializer;

        private IDictionary<MessageCommand, Type> _commandsType;
        #endregion

        #region Constructor 
        public ProtocolV1(NetworkConfig networkConfig, IBinarySerializer binarySerializer)
        {
            if (networkConfig == null) throw new ArgumentNullException(nameof(networkConfig));
            if (binarySerializer == null) throw new ArgumentNullException(nameof(binarySerializer));

            this._magic = networkConfig.Magic;
            this._binarySerializer = binarySerializer;

            this._commandsType =  new Dictionary<MessageCommand, Type>
            {
                { MessageCommand.version, typeof(VersionMessage) },
                { MessageCommand.verack, typeof(VerAckMessage) },
                { MessageCommand.addr, typeof(AddrMessage) },
                { MessageCommand.getaddr, typeof(GetAddrMessage) },
                { MessageCommand.inv, typeof(InventoryMessage) },
                { MessageCommand.headers, typeof(BlockHeadersMessage) },
                { MessageCommand.block, typeof(BlockMessage) }
            };
        }
        #endregion

        #region IProtocol Implementation 
        public uint Version => 1;

        public bool IsDefault => true;

        public async Task<Message> ReceiveMessageAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = await stream.FillBufferAsync(24, cancellationToken);

            using (var memory = new MemoryStream(buffer, false))
            {
                using (var reader = new BinaryReader(memory, Encoding.UTF8))
                {
                    var magicFromReader = reader.ReadUInt32();
                    if (magicFromReader != _magic)
                    {
                        throw new FormatException();
                    }

                    var command = Enum.Parse<MessageCommand>(Encoding.UTF8.GetString(reader.ReadBytes(12)).TrimEnd('\0'));

                    var type = this._commandsType[command];

                    var message = (Message)Activator.CreateInstance(type);
                    message.Command = command;

                    var payloadLength = reader.ReadUInt32();
                    if (payloadLength > Message.PayloadMaxSize)
                    {
                        throw new FormatException();
                    }

                    var checksum = reader.ReadUInt32();

                    var payloadBuffer = payloadLength > 0
                        ? await stream.FillBufferAsync((int) payloadLength, cancellationToken)
                        : new byte[0];

                    if (payloadBuffer.CalculateChecksum() != checksum)
                    {
                        throw new FormatException();
                    }

                    if (message is ICarryPayload messageWithPayload)
                    {
                        if (payloadLength == 0)
                        {
                            throw new FormatException();
                        }
                        // TODO #367: Prevent create the dummy object

                        messageWithPayload.Payload = BinarySerializer.Default.Deserialize(payloadBuffer, messageWithPayload.Payload.GetType());
                    }

                    return message;
                }
            }
        }

        public async Task SendMessageAsync(Stream stream, Message message, CancellationToken cancellationToken)
        {
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory, Encoding.UTF8))
                {
                    writer.Write(_magic);
                    writer.Write(Encoding.UTF8.GetBytes(message.Command.ToString().PadRight(12, '\0')));

                    var payloadBuffer = message is ICarryPayload messageWithPayload
                        ? this._binarySerializer.Serialize(messageWithPayload.Payload)
                        : new byte[0];

                    writer.Write((uint)payloadBuffer.Length);
                    writer.Write(payloadBuffer.CalculateChecksum());
                    writer.Write(payloadBuffer);
                    writer.Flush();

                    var buffer = memory.ToArray();
                    await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                }
            }
        }
        #endregion
    }
}