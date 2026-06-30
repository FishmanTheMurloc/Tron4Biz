using Protocol;

namespace Tron4Biz.Node;

public interface IFullNodeService : IDisposable
{
    TransactionExtention CreateTransaction2(TransferContract contract);

    Return BroadcastTransaction(Transaction transaction);

    TransactionExtention TriggerContract(TriggerSmartContract contract);

    TransactionExtention TriggerConstantContract(TriggerSmartContract contract);
}
