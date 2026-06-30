using Tron4Biz.Models;

namespace Tron4Biz.Tests.Mocks;

public class MockTronGrpcClient : ITronGrpcClient
{
    public string UsdtContractAddress { get; set; } = string.Empty;

    public byte[] UsdtContractBytes { get; set; } = [];

    public BlockInfo? GetNowBlockResult { get; set; }

    public CreateTransactionResult? CreateTransactionResultValue { get; set; }

    public CreateTransactionResult? CreateUsdtTransactionResultValue { get; set; }

    public CreateTransactionResult? CreateUsdtApproveResultValue { get; set; }

    public CreateTransactionResult? CreateUsdtTransferFromResultValue { get; set; }

    public long? UsdtAllowanceValue { get; set; }

    public byte[]? CallConstantResult { get; set; }

    public BroadcastResult? BroadcastResultValue { get; set; }

    public Protocol.Transaction? LastBroadcastTransaction { get; set; }

    public BlockInfo GetNowBlock()
    {
        return GetNowBlockResult ?? new BlockInfo();
    }

    public Protocol.Account GetAccount(string address)
    {
        throw new NotImplementedException();
    }

    public long GetUsdtBalance(string address)
    {
        throw new NotImplementedException();
    }

    public Protocol.Transaction GetTransactionById(string txId)
    {
        throw new NotImplementedException();
    }

    public Protocol.TransactionInfo GetTransactionInfoById(string txId)
    {
        throw new NotImplementedException();
    }

    public CreateTransactionResult CreateTransaction(string fromAddress, string toAddress, long amount)
    {
        return CreateTransactionResultValue ?? new CreateTransactionResult();
    }

    public CreateTransactionResult CreateUsdtTransaction(string fromAddress, string toAddress, long amount)
    {
        return CreateUsdtTransactionResultValue ?? new CreateTransactionResult();
    }

    public CreateTransactionResult CreateUsdtApprove(string ownerAddress, string spenderAddress, long amount)
    {
        return CreateUsdtApproveResultValue ?? new CreateTransactionResult();
    }

    public CreateTransactionResult CreateUsdtTransferFrom(string spenderAddress, string fromAddress, string toAddress, long amount)
    {
        return CreateUsdtTransferFromResultValue ?? new CreateTransactionResult();
    }

    public long GetUsdtAllowance(string ownerAddress, string spenderAddress)
    {
        return UsdtAllowanceValue ?? 0;
    }

    public byte[] CallConstantContract(string contractAddress, string ownerAddress, byte[] data)
    {
        return CallConstantResult ?? Array.Empty<byte>();
    }

    public BroadcastResult BroadcastTransaction(Protocol.Transaction signedTransaction)
    {
        LastBroadcastTransaction = signedTransaction;
        return BroadcastResultValue ?? new BroadcastResult { Success = true };
    }
}
