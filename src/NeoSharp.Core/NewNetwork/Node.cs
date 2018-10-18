using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoSharp.Core.Network;
using NeoSharp.Core.Network.Security;

namespace NeoSharp.Core.NewNetwork
{
    public class Node : INode, IDisposable
    {
        #region Private Fields 
        private readonly NetworkAcl _acl;
        private readonly IEnumerable<IPeer> _peerTypes;
        private IEnumerable<Network.EndPoint> _peerEndpoints;
        private bool _isNodeRunning;
        private CancellationTokenSource _nodeCancelationTokenSource = new CancellationTokenSource();
        private IList<NewNetwork.IPeer> _connectedPeers = new List<NewNetwork.IPeer>();
        #endregion

        #region Constructor 
        public Node(
            NetworkConfig config,
            INetworkAclLoader aclLoader, 
            IEnumerable<NewNetwork.IPeer> peerTypes)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (aclLoader == null) throw new ArgumentNullException(nameof(aclLoader));


            _acl = aclLoader.Load(config.AclConfig) ?? NetworkAcl.Default;
            _peerEndpoints = config.PeerEndPoints.ToList();
            _peerTypes = peerTypes;
        }        
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            this._nodeCancelationTokenSource.Cancel();

            foreach(var connectedPeer in this._connectedPeers)
            {
                connectedPeer.Disconnect();
            }

            this._connectedPeers = new List<NewNetwork.IPeer>(); 
        }
        #endregion

        #region INode Implementation 
        public void Start()
        {
            if (_isNodeRunning)
            {
                throw new InvalidOperationException("The node is running. To start it again please stop it before.");
            }

            this.ConnectToPeers();
        }

        public void Stop()
        {
            this.Dispose();
        }
        #endregion

        #region Private Fields 
        private void ConnectToPeers()
        {
            Parallel.ForEach (this._peerEndpoints, peerEndPoint => 
            {
                var peer = this._peerTypes
                    .Single(x => x.CanHandle(peerEndPoint.Protocol));
                peer.Connect(peerEndPoint, _nodeCancelationTokenSource);

                this._connectedPeers.Add(peer);
            });
        }
        #endregion
    }
}