using Protocol;

namespace Tron4Biz.Proto;

public interface ITronClient : IDisposable
{
    string? ApiKey { get; }

    TransactionExtention CreateTransaction2(TransferContract contract);

    Return BroadcastTransaction(Transaction transaction);

    TransactionExtention TriggerContract(TriggerSmartContract contract);

    TransactionExtention TriggerConstantContract(TriggerSmartContract contract);
}
