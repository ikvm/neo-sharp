using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network.Protocols;
using NeoSharp.TestHelpers;

namespace NeoSharp.Core.Test.Network.Protocols
{
    [TestClass]
    public class UtProtocolV1 : TestBase
    {
        [TestInitialize]
        public void WarmSerializer()
        {
            AutoMockContainer.Register<IBinarySerializer>(new BinarySerializer(typeof(VersionMessage).Assembly));
        }

        [TestMethod]
        public void Can_serialize_and_deserialize_messages()
        {
            // Arrange 
            var tcpProtocol = AutoMockContainer.Create<ProtocolV1>();
            var expectedVerAckMessage = new VerAckMessage();
            VerAckMessage actualVerAckMessage;

            // Act
            using (var memory = new MemoryStream())
            {
                tcpProtocol.SendMessage(memory, expectedVerAckMessage);
                memory.Seek(0, SeekOrigin.Begin);
                actualVerAckMessage = (VerAckMessage)tcpProtocol.ReceiveMessage(memory);
            }

            // Assert
            actualVerAckMessage
                .Should()
                .NotBeNull();
            actualVerAckMessage.Command
                .Should()
                .Be(expectedVerAckMessage.Command);
        }

        [TestMethod]
        public void Can_serialize_and_deserialize_messages_with_payload()
        {
            // Arrange 
            var versionPayload = new VersionPayload
            {
                Version = (uint)RandomInt(0, int.MaxValue),
                Services = (ulong)RandomInt(0, int.MaxValue),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Port = (ushort)RandomInt(0, short.MaxValue),
                Nonce = (uint)RandomInt(0, int.MaxValue),
                UserAgent = $"/NEO:{RandomInt(1, 10)}.{RandomInt(1, 100)}.{RandomInt(1, 1000)}/",
                CurrentBlockIndex = (uint)RandomInt(0, int.MaxValue),
                Relay = false
            };

            var tcpProtocol = AutoMockContainer.Create<ProtocolV1>();
            var expectedVersionMessage = new VersionMessage(versionPayload);
            VersionMessage actualVersionMessage;

            // Act
            using (var memory = new MemoryStream())
            {
                tcpProtocol.SendMessage(memory, expectedVersionMessage);
                memory.Seek(0, SeekOrigin.Begin);
                actualVersionMessage = (VersionMessage)tcpProtocol.ReceiveMessage(memory);
            }

            // Assert
            actualVersionMessage
                .Should()
                .NotBeNull();

            actualVersionMessage.Command
                .Should()
                .Be(expectedVersionMessage.Command);

            actualVersionMessage.Payload
                .Should()
                .NotBeNull()
                .And
                .BeEquivalentTo(versionPayload);
        }
    }
}