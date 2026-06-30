using Google.Protobuf;
using Protocol;

namespace Tron4Biz.Proto;

public interface ITronSolidityClient : IDisposable
{
    string? ApiKey { get; }

    Account GetAccount(byte[] address);

    BlockExtention GetNowBlock2();

    Transaction GetTransactionById(byte[] txId);

    TransactionInfo GetTransactionInfoById(byte[] txId);

    TransactionExtention TriggerConstantContract(TriggerSmartContract contract);
}