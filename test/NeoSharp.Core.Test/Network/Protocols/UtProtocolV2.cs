using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network.Protocols;
using NeoSharp.Core.Test.Builders;
using NeoSharp.TestHelpers;

namespace NeoSharp.Core.Test.Network.Protocols
{
    [TestClass]
    public class UtProtocolV2 : TestBase
    {
        [TestMethod]
        public void SendReceiveMessage_VerAckMessageSent_VerAckMessageReveivedIsEquivalent()
        {
            var sendedVerAckMessage = new VerAckMessage();
            Message receivedMessage;

            var testee = AutoMockContainer.Create<ProtocolV2>();

            using (var memoryStream = new MemoryStream())
            {
                testee.SendMessage(memoryStream, sendedVerAckMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                receivedMessage = testee.ReceiveMessage(memoryStream);
            }

            receivedMessage
                .Should()
                .BeOfType<VerAckMessage>()
                .And
                .NotBeNull()
                .And
                .BeEquivalentTo(sendedVerAckMessage);
        }

        [TestMethod]
        public void SendReceiveMessage_ValidVersionMessageWithZeroLengthSent_ReceiveMessageUncompressedAndIsEquivalent()
        {
            AutoMockContainer.Register<IBinarySerializer>(new BinarySerializer(typeof(VersionMessage).Assembly));

            var sendedVersionMessage = new VersionMessageBuilder()
                .WithLength(0)
                .Build();
            Message receivedMessage;

            var testee = AutoMockContainer.Create<ProtocolV2>();
            using (var memoryStream = new MemoryStream())
            {
                testee.SendMessage(memoryStream, sendedVersionMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                receivedMessage = testee.ReceiveMessage(memoryStream);
            }

            receivedMessage
                .Should()
                .BeOfType<VersionMessage>()
                .And
                .NotBeNull()
                .And
                .Match<VersionMessage>(x => !x.Flags.HasFlag(MessageFlags.Compressed))
                .And
                .BeEquivalentTo(sendedVersionMessage);
        }

        [TestMethod]
        public void SendReceiveMessage_ValidVersionMessageWith200LengthSent_ReceiveMessageIsCompressedAndEquivalent()
        {
            AutoMockContainer.Register<IBinarySerializer>(new BinarySerializer(typeof(VersionMessage).Assembly));

            var sendedVersionMessage = new VersionMessageBuilder()
                .WithLength(200)
                .Build();
            Message receivedMessage;

            var testee = this.AutoMockContainer.Create<ProtocolV2>();
            using (var memoryStream = new MemoryStream())
            {
                testee.SendMessage(memoryStream, sendedVersionMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                receivedMessage = testee.ReceiveMessage(memoryStream);
            }

            receivedMessage
                .Should()
                .BeOfType<VersionMessage>()
                .And
                .NotBeNull()
                .And
                .Match<VersionMessage>(x => x.Flags.HasFlag(MessageFlags.Compressed))
                .And
                .BeEquivalentTo(sendedVersionMessage);
        }
    }
}