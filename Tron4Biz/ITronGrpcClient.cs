using Protocol;
using Tron4Biz.Models;

namespace Tron4Biz;

public interface ITronGrpcClient
{
    string UsdtContractAddress { get; }

    byte[] UsdtContractBytes { get; }

    BlockInfo GetNowBlock();

    Account GetAccount(string address);

    long GetUsdtBalance(string address);

    Transaction GetTransactionById(string txId);

    TransactionInfo GetTransactionInfoById(string txId);

    /// <summary>
    /// 直接调用 gRPC 节点创建 TRX 转账交易。
    /// </summary>
    /// <remarks>
    /// 外部调用者应优先使用 <see cref="Transactions.ITronTransactionService.CreateTransactionAsync"/>。
    /// 此方法用于内部实现（<see cref="Transaction.GrpcTronTransactionService"/>）。
    /// </remarks>
    CreateTransactionResult CreateTransaction(string fromAddress, string toAddress, long amount);

    /// <summary>
    /// 直接调用 gRPC 节点创建 USDT 转账交易。
    /// </summary>
    /// <remarks>
    /// 外部调用者应优先使用 <see cref="Transactions.ITronTransactionService.CreateUsdtTransactionAsync"/>。
    /// 此方法用于内部实现（<see cref="Transaction.GrpcTronTransactionService"/>）。
    /// </remarks>
    CreateTransactionResult CreateUsdtTransaction(string ownerAddress, string recipientAddress, long amount);

    /// <summary>
    /// 直接调用 gRPC 节点创建 USDT 授权交易。
    /// </summary>
    /// <remarks>
    /// 外部调用者应优先使用 <see cref="Transactions.ITronTransactionService.CreateUsdtApproveAsync"/>。
    /// 此方法用于内部实现（<see cref="Transaction.GrpcTronTransactionService"/>）。
    /// </remarks>
    CreateTransactionResult CreateUsdtApprove(string ownerAddress, string spenderAddress, long amount);

    /// <summary>
    /// 直接调用 gRPC 节点创建 USDT 代付交易。
    /// </summary>
    /// <remarks>
    /// 外部调用者应优先使用 <see cref="Transactions.ITronTransactionService.CreateUsdtTransferFromAsync"/>。
    /// 此方法用于内部实现（<see cref="Transaction.GrpcTronTransactionService"/>）。
    /// </remarks>
    CreateTransactionResult CreateUsdtTransferFrom(string spenderAddress, string senderAddress, string recipientAddress, long amount);

    long GetUsdtAllowance(string ownerAddress, string spenderAddress);

    byte[] CallConstantContract(string contractAddress, string ownerAddress, byte[] data);

    BroadcastResult BroadcastTransaction(Transaction signedTransaction);
}
