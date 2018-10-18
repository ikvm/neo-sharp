using System;
using System.Collections.Generic;
using NeoSharp.Core.Models;
using NeoSharp.Types;

namespace NeoSharp.Core.Network
{
    public interface IBlockchainContext
    {
        Block CurrentBlock { get; set; }

        BlockHeader LastBlockHeader { get; set; }

        bool NeedPeerSync { get; }

        bool IsSyncing { get; set; }

        IEnumerable<IEnumerable<UInt256>> SyncBlockHeaderBatches { get; set; }

        int CurrentBlockHeaderSyncBatch { get; set; }

        bool IsPeerConnected { get; }

        void SetPeerCurrentBlockIndex(uint index);
    }
}
