using System;
using System.IO;
using System.Linq;
using NeoSharp.Core.Caching;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;

namespace NeoSharp.Core.Network.Protocols
{
    public abstract class ProtocolBase
    {
        private const int MaxBufferSize = 4096;

        protected readonly ReflectionCache<MessageCommand> Cache = ReflectionCache<MessageCommand>.CreateFromEnum<MessageCommand>();

        private readonly Type[] _highPrioritySendMessageTypes =
        {
            typeof(VersionMessage),
            typeof(VerAckMessage)
        };

        /// <summary>
        /// Magic header protocol
        /// </summary>
        public abstract uint Version { get; }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="message">Message</param>
        public abstract void SendMessage(Stream stream, Message message);

        public virtual bool IsHighPriorityMessage(Message m)
        {
            return _highPrioritySendMessageTypes.Contains(m.GetType());
        }

        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Return message or NULL</returns>
        public abstract Message ReceiveMessage(Stream stream);

        protected static byte[] FillBufferAsync(Stream stream, int size)
        {
            var buffer = new byte[Math.Min(size, MaxBufferSize)];

            using (var memory = new MemoryStream())
            {
                while (size > 0)
                {
                    var count = Math.Min(size, buffer.Length);

                    count = stream.Read(buffer, 0, count);
                    if (count <= 0) throw new IOException();

                    memory.Write(buffer, 0, count);
                    size -= count;
                }

                return memory.ToArray();
            }
        }
    }
}