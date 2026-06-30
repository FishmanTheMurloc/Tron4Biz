using Tron4Biz.Models;

namespace Tron4Biz.Transactions;

public class GrpcTronTransactionService : ITronTransactionService
{
    private readonly ITronGrpcClient _tronGrpcClient;

    public GrpcTronTransactionService(ITronGrpcClient tronGrpcClient)
    {
        _tronGrpcClient = tronGrpcClient;
    }

    public Task<CreateTransactionResult> CreateTransactionAsync(string fromAddress, string toAddress, long amount)
    {
        return Task.FromResult(_tronGrpcClient.CreateTransaction(fromAddress, toAddress, amount));
    }

    public Task<CreateTransactionResult> CreateUsdtTransactionAsync(string ownerAddress, string recipientAddress, long amount, long feeLimit)
    {
        return Task.FromResult(_tronGrpcClient.CreateUsdtTransaction(ownerAddress, recipientAddress, amount));
    }

    public Task<CreateTransactionResult> CreateUsdtApproveAsync(string ownerAddress, string spenderAddress, long amount, long feeLimit)
    {
        return Task.FromResult(_tronGrpcClient.CreateUsdtApprove(ownerAddress, spenderAddress, amount));
    }

    public Task<CreateTransactionResult> CreateUsdtTransferFromAsync(string spenderAddress, string senderAddress, string recipientAddress, long amount, long feeLimit)
    {
        return Task.FromResult(_tronGrpcClient.CreateUsdtTransferFrom(spenderAddress, senderAddress, recipientAddress, amount));
    }
}
