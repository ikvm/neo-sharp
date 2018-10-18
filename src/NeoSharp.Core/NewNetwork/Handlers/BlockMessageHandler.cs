using System;
using System.Linq;
using System.Threading.Tasks;
using NeoSharp.Core.Blockchain.Processing;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Models.OperationManger;
using NeoSharp.Core.Network;

namespace NeoSharp.Core.NewNetwork.Handlers
{
    public class BlockMessageHandler : IMessageHandler
    {
        #region Private Fields 
        private readonly IBlockProcessor _blockProcessor;
        private IBlockOperationsManager _blockOperationsManager;
        private readonly IBlockchainContext _blockchainContext;
        private readonly ILogger<BlockMessageHandler> _logger;
        #endregion

        #region Constructor
        public BlockMessageHandler(
            IBlockProcessor blockProcessor,
            IBlockOperationsManager blockOperationsManager,
            IBlockchainContext blockChainContext,
            ILogger<BlockMessageHandler> logger)
        {
            this._blockProcessor = blockProcessor ?? throw new ArgumentNullException(nameof(blockProcessor));
            this._blockOperationsManager = blockOperationsManager ?? throw new ArgumentNullException(nameof(blockOperationsManager));
            this._blockchainContext = blockChainContext;
            this._logger = logger;
        }
        #endregion

        #region IMessageHandler implementation 
        public bool CanHandle(Message message)
        {
            return message is BlockMessage;
        }

        public async Task Handle(Message message, IPeer sourcePeer)
        {
            var block = ((BlockMessage)message).Payload;

            if (block.Hash == null)
            {
                this._blockOperationsManager.Sign(block);
            }

            if (this._blockOperationsManager.Verify(block))
            {
                // TODO [AboimPinto]: This will be added later
                //_logger.LogInformation($"Broadcasting block {block.Hash} with Index {block.Index}.");
                //_broadcaster.Broadcast(message, sender);

                await this._blockProcessor.AddBlock(block);

                if (this._blockchainContext.IsSyncing)
                {
                    var currentBlockBatch = this._blockchainContext.SyncBlockHeaderBatches.ElementAt(this._blockchainContext.CurrentBlockHeaderSyncBatch);
                    if (block.Hash == currentBlockBatch.Last())
                    {
                        // is the last block of the batch. Request more blocks
                        this._blockchainContext.CurrentBlockHeaderSyncBatch++;

                        if (this._blockchainContext.CurrentBlockHeaderSyncBatch < this._blockchainContext.SyncBlockHeaderBatches.Count())
                        {
                            currentBlockBatch = this._blockchainContext.SyncBlockHeaderBatches.ElementAt(this._blockchainContext.CurrentBlockHeaderSyncBatch);

                            sourcePeer.QueueMessageToSend(new GetDataMessage(InventoryType.Block, currentBlockBatch)); 
                        }
                        else
                        {
                            sourcePeer.QueueMessageToSend(new GetBlockHeadersMessage(block.Hash));
                        }
                    }
                }
            }
            else
            {
                this._logger.LogError($"Block {block.Hash} with Index {block.Index} verification fail.");
            }
        }
        #endregion
    }
}