using System.Security.Cryptography;
using Google.Protobuf;
using Microsoft.Extensions.Time.Testing;
using Tron4Biz.Models;
using Tron4Biz.Tests.Mocks;
using Tron4Biz.Transactions;
using Tron4Biz.Crypto;
using Xunit;
using Protocol;

namespace Tron4Biz.Tests.Transactions;

public class LocalTronTransactionServiceTests
{
    private static readonly BlockInfo _knownBlockInfo = new()
    {
        BlockNumber = 1,
        BlockTimestamp = 1775336080333,
        RefBlockBytes = [0x95, 0x41],
        RefBlockHash = [0x7e, 0xe9, 0x42, 0xd6, 0x74, 0x2f, 0x00, 0xcb]
    };

    private const string KnownFromHex = "41fd49eda0f23ff7ec1d03b52c3a45991c24cd440e";
    private const string KnownToHex = "4198927ffb9f554dc4a453c64b2e553a02d6df514b";
    private const string UsdtContractAddress = "TG3XXyExBkPp9nzdajDZsozEu4BkaSJozs";

    private static MockTronGrpcClient CreateMockTronGrpcClient()
    {
        var mock = new MockTronGrpcClient();
        mock.GetNowBlockResult = _knownBlockInfo;
        return mock;
    }

    private static MockTronGrpcClient CreateMockTronGrpcClientWithUsdt()
    {
        var mock = new MockTronGrpcClient
        {
            UsdtContractAddress = UsdtContractAddress,
            UsdtContractBytes = TronAddress.FromBase58(UsdtContractAddress).AddressBytes,
            GetNowBlockResult = _knownBlockInfo
        };
        return mock;
    }

    private static LocalTronTransactionService CreateService(MockTronGrpcClient mock, DateTimeOffset? now = null)
    {
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(now ?? DateTimeOffset.FromUnixTimeMilliseconds(1775336080333));
        return new LocalTronTransactionService(mock, fakeTime, 60_000);
    }

    private static string NewAddress()
    {
        return TronKey.Generate().Address.Base58Check;
    }

    [Fact]
    public async Task CreateTransactionAsync_TxIdIsValidFormat()
    {
        var mockTron = CreateMockTronGrpcClient();
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.FromUnixTimeMilliseconds(1775336080333));
        var txService = new LocalTronTransactionService(mockTron, fakeTime, 58_667);

        var fromAddress = TronAddress.FromHex(KnownFromHex).Base58Check;
        var toAddress = TronAddress.FromHex(KnownToHex).Base58Check;

        var result = await txService.CreateTransactionAsync(fromAddress, toAddress, 1000);

        Assert.Equal("83823c5f5a1d33633938d05cb99c20a5fbb9c7dc6bfd9c8d94ac75d3e72ff975", result.TxId);
    }

    [Fact]
    public async Task CreateTransactionAsync_TxIdIsDeterministic()
    {
        var mockTron = CreateMockTronGrpcClient();
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.FromUnixTimeMilliseconds(1775336080333));
        var txService = new LocalTronTransactionService(mockTron, fakeTime, 58_667);

        var fromAddress = TronAddress.FromHex(KnownFromHex).Base58Check;
        var toAddress = TronAddress.FromHex(KnownToHex).Base58Check;

        var result1 = await txService.CreateTransactionAsync(fromAddress, toAddress, 1000);
        var result2 = await txService.CreateTransactionAsync(fromAddress, toAddress, 1000);

        Assert.Equal(result1.TxId, result2.TxId);
    }

    [Fact]
    public async Task CreateTransactionAsync_DifferentAmountsProduceDifferentTxIds()
    {
        var mockTron = CreateMockTronGrpcClient();
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.FromUnixTimeMilliseconds(1775336080333));
        var txService = new LocalTronTransactionService(mockTron, fakeTime, 58_667);

        var fromAddress = TronAddress.FromHex(KnownFromHex).Base58Check;
        var toAddress = TronAddress.FromHex(KnownToHex).Base58Check;

        var result1 = await txService.CreateTransactionAsync(fromAddress, toAddress, 1000);
        var result2 = await txService.CreateTransactionAsync(fromAddress, toAddress, 2000);

        Assert.NotEqual(result1.TxId, result2.TxId);
    }

    [Fact]
    public async Task CreateTransactionAsync_GeneratesCorrectTransactionData()
    {
        var blockInfo = new BlockInfo
        {
            BlockNumber = 1,
            BlockTimestamp = 1000,
            RefBlockBytes = [0x00, 0x00, 0x00, 0x01],
            RefBlockHash = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]
        };

        var mockTron = new MockTronGrpcClient();
        mockTron.GetNowBlockResult = blockInfo;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(now);

        var fromAddress = TronKey.Generate().Address.Base58Check;
        var toAddress = TronKey.Generate().Address.Base58Check;

        var localTxService = new LocalTronTransactionService(mockTron, fakeTime);
        var result = await localTxService.CreateTransactionAsync(fromAddress, toAddress, 1000);

        Assert.NotNull(result);
        Assert.NotNull(result.TxId);
        Assert.Equal(64, result.TxId.Length);

        var tx = result.Transaction;
        var rawData = tx.RawData;

        Assert.Equal(blockInfo.RefBlockBytes, rawData.RefBlockBytes.ToByteArray());
        Assert.Equal(blockInfo.RefBlockHash, rawData.RefBlockHash.ToByteArray());
        Assert.Equal(now.ToUnixTimeMilliseconds() + 60_000, rawData.Expiration);

        var contract = rawData.Contract[0];
        Assert.Equal(Transaction.Types.Contract.Types.ContractType.TransferContract, contract.Type);

        var transferContract = TransferContract.Parser.ParseFrom(contract.Parameter.Value);
        Assert.Equal(1000, transferContract.Amount);

        var expectedTxId = SHA256.HashData(rawData.ToByteArray());
        Assert.Equal(Convert.ToHexString(expectedTxId).ToLowerInvariant(), result.TxId);
    }

    [Fact]
    public async Task CreateUsdtApproveAsync_ProducesTriggerSmartContractWithApproveSelector()
    {
        var mock = CreateMockTronGrpcClientWithUsdt();
        var service = CreateService(mock);
        var owner = NewAddress();
        var spender = NewAddress();

        var result = await service.CreateUsdtApproveAsync(owner, spender, 1_000_000, feeLimit: 50_000_000);

        Assert.NotNull(result);
        Assert.Equal(64, result.TxId.Length);

        var tx = result.Transaction;
        Assert.Single(tx.RawData.Contract);
        var raw = tx.RawData.Contract[0];
        Assert.Equal(Transaction.Types.Contract.Types.ContractType.TriggerSmartContract, raw.Type);

        var trigger = TriggerSmartContract.Parser.ParseFrom(raw.Parameter.Value);
        var data = trigger.Data.ToByteArray();

        Assert.Equal(AbiEncoder.ApproveSelector, data.Take(4).ToArray());

        var ownerBytes = TronAddress.FromBase58(owner).AddressBytes;
        Assert.Equal(ownerBytes, trigger.OwnerAddress.ToByteArray());
        Assert.Equal(mock.UsdtContractBytes, trigger.ContractAddress.ToByteArray());
        Assert.Equal(50_000_000, tx.RawData.FeeLimit);
    }

    [Fact]
    public async Task CreateUsdtApproveAsync_TxIdIsDeterministic()
    {
        var mock = CreateMockTronGrpcClientWithUsdt();
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1775336080333);
        var owner = NewAddress();
        var spender = NewAddress();

        var r1 = await CreateService(mock, now).CreateUsdtApproveAsync(owner, spender, 100, 1_000_000);
        var r2 = await CreateService(mock, now).CreateUsdtApproveAsync(owner, spender, 100, 1_000_000);

        Assert.Equal(r1.TxId, r2.TxId);
    }

    [Fact]
    public async Task CreateUsdtTransferFromAsync_OwnerIsSpender()
    {
        var mock = CreateMockTronGrpcClientWithUsdt();
        var service = CreateService(mock);
        var spender = NewAddress();
        var sender = NewAddress();
        var recipient = NewAddress();

        var result = await service.CreateUsdtTransferFromAsync(spender, sender, recipient, 5_000_000, feeLimit: 80_000_000);

        var tx = result.Transaction;
        var trigger = TriggerSmartContract.Parser.ParseFrom(tx.RawData.Contract[0].Parameter.Value);
        var data = trigger.Data.ToByteArray();

        Assert.Equal(AbiEncoder.TransferFromSelector, data.Take(4).ToArray());

        var spenderBytes = TronAddress.FromBase58(spender).AddressBytes;
        Assert.Equal(spenderBytes, trigger.OwnerAddress.ToByteArray());
        Assert.Equal(80_000_000, tx.RawData.FeeLimit);
    }

    [Fact]
    public async Task CreateUsdtTransferFromAsync_EncodesAllThreeArguments()
    {
        var mock = CreateMockTronGrpcClientWithUsdt();
        var service = CreateService(mock);
        var spender = NewAddress();
        var sender = NewAddress();
        var recipient = NewAddress();

        var result = await service.CreateUsdtTransferFromAsync(spender, sender, recipient, 12345, 0);
        var tx = result.Transaction;
        var trigger = TriggerSmartContract.Parser.ParseFrom(tx.RawData.Contract[0].Parameter.Value);
        var data = trigger.Data.ToByteArray();

        Assert.Equal(100, data.Length);
        Assert.Equal(AbiEncoder.TransferFromSelector, data.Take(4).ToArray());

        var senderBytes = TronAddress.FromBase58(sender).AddressBytes.Skip(1).ToArray();
        var recipientBytes = TronAddress.FromBase58(recipient).AddressBytes.Skip(1).ToArray();

        var encodedSender = data.Skip(16).Take(20).ToArray();
        var encodedRecipient = data.Skip(48).Take(20).ToArray();

        Assert.Equal(senderBytes, encodedSender);
        Assert.Equal(recipientBytes, encodedRecipient);
        Assert.Equal(12345L, AbiEncoder.DecodeUint256(data.Skip(68).Take(32).ToArray()));
    }
}
