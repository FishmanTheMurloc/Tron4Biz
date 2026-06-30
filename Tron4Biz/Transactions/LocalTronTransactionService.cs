using Google.Protobuf;
using Tron4Biz.Models;
using Protocol;
using Tron4Biz.Crypto;

namespace Tron4Biz.Transactions;

public class LocalTronTransactionService : ITronTransactionService
{
    private readonly ITronGrpcClient _tronGrpcClient;
    private readonly TimeProvider _timeProvider;
    private readonly long _expirationDeltaMs;

    public LocalTronTransactionService(ITronGrpcClient tronGrpcClient, TimeProvider? timeProvider = null, long expirationDeltaMs = 60_000)
    {
        _tronGrpcClient = tronGrpcClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _expirationDeltaMs = expirationDeltaMs;
    }

    public Task<CreateTransactionResult> CreateTransactionAsync(string fromAddress, string toAddress, long amount)
    {
        var fromBytes = TronAddress.FromBase58(fromAddress).AddressBytes;
        var toBytes = TronAddress.FromBase58(toAddress).AddressBytes;

        var contract = BuildTransferContract(fromBytes, toBytes, amount);
        return Task.FromResult(BuildTriggeredTransaction(contract, feeLimit: 0));
    }

    public Task<CreateTransactionResult> CreateUsdtTransactionAsync(string ownerAddress, string recipientAddress, long amount, long feeLimit)
    {
        var ownerBytes = TronAddress.FromBase58(ownerAddress).AddressBytes;
        var contractBytes = _tronGrpcClient.UsdtContractBytes;
        var callData = AbiEncoder.EncodeTransfer(recipientAddress, amount);

        var contract = BuildTriggerSmartContract(ownerBytes, contractBytes.ToArray(), callData);
        return Task.FromResult(BuildTriggeredTransaction(contract, feeLimit));
    }

    public Task<CreateTransactionResult> CreateUsdtApproveAsync(string ownerAddress, string spenderAddress, long amount, long feeLimit)
    {
        var ownerBytes = TronAddress.FromBase58(ownerAddress).AddressBytes;
        var contractBytes = _tronGrpcClient.UsdtContractBytes;
        var callData = AbiEncoder.EncodeApprove(spenderAddress, amount);

        var contract = BuildTriggerSmartContract(ownerBytes, contractBytes.ToArray(), callData);
        return Task.FromResult(BuildTriggeredTransaction(contract, feeLimit));
    }

    public Task<CreateTransactionResult> CreateUsdtTransferFromAsync(string spenderAddress, string senderAddress, string recipientAddress, long amount, long feeLimit)
    {
        var spenderBytes = TronAddress.FromBase58(spenderAddress).AddressBytes;
        var contractBytes = _tronGrpcClient.UsdtContractBytes;
        var callData = AbiEncoder.EncodeTransferFrom(senderAddress, recipientAddress, amount);

        var contract = BuildTriggerSmartContract(spenderBytes, contractBytes.ToArray(), callData);
        return Task.FromResult(BuildTriggeredTransaction(contract, feeLimit));
    }

    private CreateTransactionResult BuildTriggeredTransaction(IMessage contract, long feeLimit)
    {
        var blockInfo = _tronGrpcClient.GetNowBlock();
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        var expiration = timestamp + _expirationDeltaMs;

        var rawData = BuildRawData(blockInfo.RefBlockBytes, blockInfo.RefBlockHash, expiration, timestamp, contract, feeLimit);
        var txId = CalculateTxId(rawData);
        var transaction = BuildTransaction(rawData);

        return new CreateTransactionResult
        {
            Transaction = transaction,
            TxId = txId
        };
    }

    private static TransferContract BuildTransferContract(byte[] fromAddress, byte[] toAddress, long amount)
    {
        return new TransferContract
        {
            OwnerAddress = ByteString.CopyFrom(fromAddress),
            ToAddress = ByteString.CopyFrom(toAddress),
            Amount = amount
        };
    }

    private static TriggerSmartContract BuildTriggerSmartContract(byte[] ownerAddress, byte[] contractAddress, byte[] callData)
    {
        return new TriggerSmartContract
        {
            OwnerAddress = ByteString.CopyFrom(ownerAddress),
            ContractAddress = ByteString.CopyFrom(contractAddress),
            Data = ByteString.CopyFrom(callData),
            CallValue = 0
        };
    }

    private static Transaction.Types.raw BuildRawData(
        byte[] refBlockBytes,
        byte[] refBlockHash,
        long expiration,
        long timestamp,
        IMessage contract,
        long feeLimit = 0)
    {
        var rawData = new Transaction.Types.raw
        {
            RefBlockBytes = ByteString.CopyFrom(refBlockBytes),
            RefBlockHash = ByteString.CopyFrom(refBlockHash),
            Expiration = expiration,
            Timestamp = timestamp,
            FeeLimit = feeLimit
        };

        var contractType = contract switch
        {
            TransferContract => Transaction.Types.Contract.Types.ContractType.TransferContract,
            TriggerSmartContract => Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,
            _ => throw new NotSupportedException($"Contract type {contract.GetType()} not supported")
        };

        var typeUrl = contractType switch
        {
            Transaction.Types.Contract.Types.ContractType.TransferContract => "type.googleapis.com/protocol.TransferContract",
            Transaction.Types.Contract.Types.ContractType.TriggerSmartContract => "type.googleapis.com/protocol.TriggerSmartContract",
            _ => throw new NotSupportedException($"Contract type {contractType} not supported")
        };

        rawData.Contract.Add(new Transaction.Types.Contract
        {
            Type = contractType,
            Parameter = new Google.Protobuf.WellKnownTypes.Any
            {
                TypeUrl = typeUrl,
                Value = contract.ToByteString()
            }
        });

        return rawData;
    }

    private static string CalculateTxId(Transaction.Types.raw rawData)
    {
        var txIdBytes = System.Security.Cryptography.SHA256.HashData(rawData.ToByteArray());
        return Convert.ToHexString(txIdBytes).ToLowerInvariant();
    }

    private static Transaction BuildTransaction(Transaction.Types.raw rawData)
    {
        return new Transaction { RawData = rawData };
    }
}
