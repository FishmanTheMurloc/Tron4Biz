using Google.Protobuf;
using Protocol;

namespace Tron4Biz.Node;

public interface ISolidityNodeService : IDisposable
{
    Account GetAccount(byte[] address);

    BlockExtention GetNowBlock2();

    Transaction GetTransactionById(byte[] txId);

    TransactionInfo GetTransactionInfoById(byte[] txId);

    TransactionExtention TriggerConstantContract(TriggerSmartContract contract);
}
