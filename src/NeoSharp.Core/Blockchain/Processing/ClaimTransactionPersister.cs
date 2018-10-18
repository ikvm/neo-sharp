using System;
using System.Linq;
using System.Threading.Tasks;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Models;
using NeoSharp.Core.Persistence;

namespace NeoSharp.Core.Blockchain.Processing
{
    /// <summary>
    /// Special processing for ClaimTransactions.
    /// The coin states that are claimed are marked as CoinState.Claimed.
    /// The increment of GAS of the account is done by the TransactionPersister.
    /// </summary>
    public class ClaimTransactionPersister: ITransactionPersister<ClaimTransaction>
    {
        private readonly IRepository _repository;
        private readonly ILogger<ClaimTransactionPersister> _logger;

        public ClaimTransactionPersister(IRepository repository, ILogger<ClaimTransactionPersister> logger)
        {
            this._repository = repository;
            this._logger = logger;
        }

        public async Task Persist(ClaimTransaction transaction)
        {
            try
            {
                foreach (var prevHashClaim in transaction.Claims.GroupBy(c => c.PrevHash))
                {
                    var coinStates = await _repository.GetCoinStates(prevHashClaim.Key);
                    foreach (var reference in prevHashClaim) coinStates[reference.PrevIndex] |= CoinState.Claimed;
                    await _repository.AddCoinStates(prevHashClaim.Key, coinStates);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error persisting a claim transaction");
            }
        }
    }
}