using Tron4Biz.Models;

namespace Tron4Biz.Transactions;

public interface ITronTransactionService
{
    Task<CreateTransactionResult> CreateTransactionAsync(string fromAddress, string toAddress, long amount);

    Task<CreateTransactionResult> CreateUsdtTransactionAsync(string ownerAddress, string recipientAddress, long amount, long feeLimit);

    Task<CreateTransactionResult> CreateUsdtApproveAsync(string ownerAddress, string spenderAddress, long amount, long feeLimit);

    Task<CreateTransactionResult> CreateUsdtTransferFromAsync(string spenderAddress, string senderAddress, string recipientAddress, long amount, long feeLimit);
}
