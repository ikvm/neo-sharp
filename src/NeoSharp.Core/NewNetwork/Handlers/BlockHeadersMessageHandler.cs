using System;
using System.Linq;
using System.Threading.Tasks;
using NeoSharp.Core.Blockchain.Processing;
using NeoSharp.Core.Blockchain.Repositories;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Models;
using NeoSharp.Core.Network;

namespace NeoSharp.Core.NewNetwork.Handlers
{
    public class BlockHeadersMessageHandler : IMessageHandler
    {
        #region Private Fields 
        private readonly IBlockPersister _blockPersister;
        private readonly IBlockRepository _blockRepository;
        private readonly IBlockchainContext _blockchainContext;
        #endregion

        #region Constructor 
        public BlockHeadersMessageHandler(
            IBlockPersister blockPersister,
            IBlockRepository blockRepository,
            IBlockchainContext blockchainContext)
        {
            this._blockPersister = blockPersister ?? throw new ArgumentNullException(nameof(blockPersister));
            this._blockRepository = blockRepository;
            this._blockchainContext = blockchainContext;
        }
        #endregion

        #region IMessageHandler Implementation 
        public bool CanHandle(Message message)
        {
            return message is BlockHeadersMessage;
        }

        public async Task Handle(Message message, IPeer sourcePeer)
        {
            var blockHeadersMessage = (BlockHeadersMessage) message;
            blockHeadersMessage.Payload.Headers.ForEach(a => a.Type = HeaderType.Header);

            var count = blockHeadersMessage.Payload.Headers.Length;

            await this._blockPersister.Persist(blockHeadersMessage.Payload.Headers ?? new BlockHeader[0]);

            var startBlockToSync = this._blockchainContext.LastBlockHeader.Index;

            var blockSyncOffSet = this._blockchainContext.LastBlockHeader.Index - this._blockchainContext.CurrentBlock.Index;

            var syncFirstBatch = false;
            if (blockSyncOffSet > 2000)
            {
                // Resume algorithm. When the node stop in the middle of the Sync and start again, this code get the Hashes from the block headers already sync and request their blocks.
                startBlockToSync = this._blockchainContext.CurrentBlock.Index;

                var blockHahesToSync = await this._blockRepository.GetBlockHashes(startBlockToSync, this._blockchainContext.CurrentBlock.Index);

                if (blockHahesToSync.Any())
                {
                    var blockHahesBatchesToSync = blockHahesToSync
                        .Batch(500);

                    this._blockchainContext.SyncBlockHeaderBatches = blockHahesBatchesToSync;
                    syncFirstBatch = true;
                }
            }
            else
            {
                if (startBlockToSync < sourcePeer.Version.CurrentBlockIndex)
                {
                    var blockHeaderBatches = blockHeadersMessage.Payload.Headers
                        .Select(x => x.Hash)
                        .Batch(500);

                    this._blockchainContext.SyncBlockHeaderBatches = blockHeaderBatches;
                    syncFirstBatch = true;
                }
                else
                {
                    // Sync is over. Next block should be from the consensus
                    this._blockchainContext.IsSyncing = false;
                    syncFirstBatch = false;
                }
            }

            if (syncFirstBatch)
            {
                this._blockchainContext.CurrentBlockHeaderSyncBatch = 0;

                var hashes = this._blockchainContext.SyncBlockHeaderBatches
                    .ElementAt(this._blockchainContext.CurrentBlockHeaderSyncBatch);
                sourcePeer.QueueMessageToSend(new GetDataMessage(InventoryType.Block, hashes));

                this._blockchainContext.IsSyncing = true;
            }
        }
        #endregion
    }
}