using System.Linq;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Network;
using NeoSharp.Core.Network.Protocols;
using NeoSharp.Core.Network.Rpc;
using NeoSharp.Core.Network.Security;
using NeoSharp.Core.Network.Tcp;

namespace NeoSharp.Core.DI.Modules
{
    public class NetworkModule : IModule
    {
        public void Register(IContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterSingleton<IServerContext, ServerContext>();
            containerBuilder.RegisterSingleton<IBlockchainContext, BlockchainContext>();

            containerBuilder.RegisterSingleton<NewNetwork.INode, NewNetwork.Node>();

            var peerTypes = typeof(NewNetwork.IPeer).Assembly
               .GetExportedTypes()
               .Where(t => t.IsClass && !t.IsInterface && !t.IsAbstract && typeof(NewNetwork.IPeer).IsAssignableFrom(t))
               .ToArray();
            containerBuilder.RegisterCollection(typeof(NewNetwork.IPeer), peerTypes);

            containerBuilder.RegisterCollection(
                typeof(NewNetwork.Protocols.IProtocol), 
                new[] 
                { 
                    typeof(NewNetwork.Protocols.ProtocolV1), 
                    typeof(NewNetwork.Protocols.ProtocolV2) 
                });

            containerBuilder.RegisterCollection(
                typeof(NewNetwork.Handlers.IMessageHandler), 
                new[]
                {
                    typeof(NewNetwork.Handlers.VersionMessageHandler),
                    typeof(NewNetwork.Handlers.VerAckMessageHandler),
                    typeof(NewNetwork.Handlers.AddrMessageMessageHandler),
                    typeof(NewNetwork.Handlers.GetAddrMessageHandler),
                    typeof(NewNetwork.Handlers.InventoryMessageHandler),
                    typeof(NewNetwork.Handlers.BlockHeadersMessageHandler),
                    typeof(NewNetwork.Handlers.BlockMessageHandler)
                });

            containerBuilder.RegisterSingleton<IPeerMessageListener, PeerMessageListener>();

            containerBuilder.RegisterSingleton<NetworkConfig>();
            containerBuilder.RegisterSingleton<RpcConfig>();
            containerBuilder.RegisterSingleton<ProtocolSelector>();
            containerBuilder.RegisterSingleton<INetworkAclLoader, NetworkAclLoader>();
            containerBuilder.RegisterSingleton<INetworkManager, NetworkManager>();
            containerBuilder.RegisterSingleton<IBroadcaster, Server>();
            // containerBuilder.RegisterSingleton<IServer, Server>();
            containerBuilder.RegisterSingleton<IRpcServer, RpcServer>();
            containerBuilder.RegisterSingleton<IPeerFactory, PeerFactory>();
            containerBuilder.RegisterSingleton<IPeerListener, TcpPeerListener>();
            containerBuilder.RegisterSingleton<ITcpPeerFactory, TcpPeerFactory>();

            containerBuilder.RegisterCollectionOf<IMessageHandler>();
            containerBuilder.RegisterSingleton<IMessageHandlerProxy, MessageHandlerProxy>();
        }
    }
}