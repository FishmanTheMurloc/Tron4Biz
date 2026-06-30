using Google.Protobuf;
using Grpc.Core;
using Protocol;
using Tron4Biz.Crypto;
using Tron4Biz.Models;
using Tron4Biz.Options;
using Tron4Biz.Node;

namespace Tron4Biz;

public class TronGrpcClient : ITronGrpcClient
{
    private readonly IFullNodeService _fullNode;
    private readonly ISolidityNodeService _solidityNode;
    private readonly Tron4BizOptions _options;

    public string UsdtContractAddress => _options.UsdtContractAddress;

    public byte[] UsdtContractBytes => TronAddress.FromBase58(_options.UsdtContractAddress).AddressBytes;

    public TronGrpcClient(IFullNodeService fullNode, ISolidityNodeService solidityNode, Tron4BizOptions options)
    {
        _fullNode = fullNode;
        _solidityNode = solidityNode;
        _options = options;
    }

    public BlockInfo GetNowBlock()
    {
        var block = _solidityNode.GetNowBlock2();
        var rawData = block.BlockHeader.RawData;
        var blockIdBytes = block.Blockid.ToByteArray();

        var blockNumber = rawData.Number;
        var blockNumBytes = BitConverter.GetBytes(blockNumber);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(blockNumBytes);

        var refBlockBytes = new byte[2];
        refBlockBytes[0] = blockNumBytes[6];
        refBlockBytes[1] = blockNumBytes[7];

        var refBlockHash = new byte[8];
        Array.Copy(blockIdBytes, 8, refBlockHash, 0, 8);

        return new BlockInfo
        {
            BlockNumber = rawData.Number,
            BlockTimestamp = rawData.Timestamp,
            BlockHash = blockIdBytes,
            RefBlockBytes = refBlockBytes,
            RefBlockHash = refBlockHash
        };
    }

    public Account GetAccount(string address)
    {
        var addressBytes = TronAddress.FromBase58(address).AddressBytes;
        return _solidityNode.GetAccount(addressBytes);
    }

    public long GetUsdtBalance(string address)
    {
        var data = CallConstantContract(UsdtContractAddress, address, AbiEncoder.EncodeBalanceOf(address));
        return data.Length == 0 ? 0 : AbiEncoder.DecodeUint256(data);
    }

    public Transaction GetTransactionById(string txId)
    {
        var txIdBytes = Convert.FromHexString(txId);
        return _solidityNode.GetTransactionById(txIdBytes);
    }

    public TransactionInfo GetTransactionInfoById(string txId)
    {
        var txIdBytes = Convert.FromHexString(txId);
        return _solidityNode.GetTransactionInfoById(txIdBytes);
    }

    public CreateTransactionResult CreateTransaction(string fromAddress, string toAddress, long amount)
    {
        var fromBytes = TronAddress.FromBase58(fromAddress).AddressBytes;
        var toBytes = TronAddress.FromBase58(toAddress).AddressBytes;

        var contract = new TransferContract
        {
            OwnerAddress = ByteString.CopyFrom(fromBytes),
            ToAddress = ByteString.CopyFrom(toBytes),
            Amount = amount
        };

        var result = _fullNode.CreateTransaction2(contract);

        return new CreateTransactionResult
        {
            Transaction = result.Transaction,
            TxId = Convert.ToHexString(result.Txid.ToByteArray()).ToLowerInvariant()
        };
    }

    public CreateTransactionResult CreateUsdtTransaction(string ownerAddress, string recipientAddress, long amount)
    {
        var ownerBytes = TronAddress.FromBase58(ownerAddress).AddressBytes;
        var callData = AbiEncoder.EncodeTransfer(recipientAddress, amount);

        var trigger = new TriggerSmartContract
        {
            OwnerAddress = ByteString.CopyFrom(ownerBytes),
            ContractAddress = ByteString.CopyFrom(UsdtContractBytes),
            CallValue = 0,
            Data = ByteString.CopyFrom(callData)
        };

        var result = _fullNode.TriggerContract(trigger);

        return new CreateTransactionResult
        {
            Transaction = result.Transaction,
            TxId = Convert.ToHexString(result.Txid.ToByteArray()).ToLowerInvariant()
        };
    }

    public CreateTransactionResult CreateUsdtApprove(string ownerAddress, string spenderAddress, long amount)
    {
        var ownerBytes = TronAddress.FromBase58(ownerAddress).AddressBytes;
        var callData = AbiEncoder.EncodeApprove(spenderAddress, amount);

        var trigger = new TriggerSmartContract
        {
            OwnerAddress = ByteString.CopyFrom(ownerBytes),
            ContractAddress = ByteString.CopyFrom(UsdtContractBytes),
            CallValue = 0,
            Data = ByteString.CopyFrom(callData)
        };

        var result = _fullNode.TriggerContract(trigger);

        return new CreateTransactionResult
        {
            Transaction = result.Transaction,
            TxId = Convert.ToHexString(result.Txid.ToByteArray()).ToLowerInvariant()
        };
    }

    public CreateTransactionResult CreateUsdtTransferFrom(string spenderAddress, string senderAddress, string recipientAddress, long amount)
    {
        var spenderBytes = TronAddress.FromBase58(spenderAddress).AddressBytes;
        var callData = AbiEncoder.EncodeTransferFrom(senderAddress, recipientAddress, amount);

        var trigger = new TriggerSmartContract
        {
            OwnerAddress = ByteString.CopyFrom(spenderBytes),
            ContractAddress = ByteString.CopyFrom(UsdtContractBytes),
            CallValue = 0,
            Data = ByteString.CopyFrom(callData)
        };

        var result = _fullNode.TriggerContract(trigger);

        return new CreateTransactionResult
        {
            Transaction = result.Transaction,
            TxId = Convert.ToHexString(result.Txid.ToByteArray()).ToLowerInvariant()
        };
    }

    public long GetUsdtAllowance(string ownerAddress, string spenderAddress)
    {
        var data = CallConstantContract(UsdtContractAddress, ownerAddress, AbiEncoder.EncodeAllowance(ownerAddress, spenderAddress));
        return AbiEncoder.DecodeUint256(data);
    }

    public byte[] CallConstantContract(string contractAddress, string ownerAddress, byte[] data)
    {
        var contractBytes = TronAddress.FromBase58(contractAddress).AddressBytes;
        var ownerBytes = TronAddress.FromBase58(ownerAddress).AddressBytes;

        var trigger = new TriggerSmartContract
        {
            OwnerAddress = ByteString.CopyFrom(ownerBytes),
            ContractAddress = ByteString.CopyFrom(contractBytes),
            CallValue = 0,
            Data = ByteString.CopyFrom(data)
        };

        var result = _solidityNode.TriggerConstantContract(trigger);

        if (result.Result != null && !result.Result.Result)
        {
            throw new InvalidOperationException($"Constant call failed: {result.Result.Code} {result.Result.Message.ToStringUtf8()}");
        }

        if (result.ConstantResult == null || result.ConstantResult.Count == 0)
        {
            return Array.Empty<byte>();
        }

        return result.ConstantResult[0].ToByteArray();
    }

    public BroadcastResult BroadcastTransaction(Transaction signedTransaction)
    {
        try
        {
            var result = _fullNode.BroadcastTransaction(signedTransaction);

            return new BroadcastResult
            {
                Success = result.Result,
                Code = (BroadcastStatusCode)result.Code,
                Message = result.Message.ToStringUtf8()
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            return new BroadcastResult
            {
                Success = false,
                Code = BroadcastStatusCode.NetworkTimeout,
                Message = $"Broadcast timeout: {ex.Message}"
            };
        }
        catch (RpcException ex)
        {
            return new BroadcastResult
            {
                Success = false,
                Code = BroadcastStatusCode.NetworkError,
                Message = $"Network error ({ex.StatusCode}): {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new BroadcastResult
            {
                Success = false,
                Code = BroadcastStatusCode.NetworkError,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
