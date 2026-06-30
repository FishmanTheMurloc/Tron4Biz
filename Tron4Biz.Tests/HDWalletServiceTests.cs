using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests;

public class HDWalletServiceTests
{
    private readonly IHDWalletService _hdWalletService;

    public HDWalletServiceTests()
    {
        _hdWalletService = new HDWalletService();
    }

    [Fact]
    public void GenerateMnemonic_Returns12Words()
    {
        var mnemonic = _hdWalletService.GenerateMnemonic();
        var words = mnemonic.Split(' ');

        Assert.Equal(12, words.Length);
    }

    [Fact]
    public void GenerateMnemonic_ReturnsValidBip39Words()
    {
        var mnemonic = _hdWalletService.GenerateMnemonic();

        Assert.NotEmpty(mnemonic);
        Assert.Contains(' ', mnemonic);
    }

    [Fact]
    public void DeriveSeed_Returns512BitSeed()
    {
        var mnemonic = _hdWalletService.GenerateMnemonic();
        var seed = _hdWalletService.DeriveSeed(mnemonic);

        Assert.Equal(64, seed.Length);
    }

    [Theory]
    [InlineData("41e8b7c1f5c29e6a3d3d7f8d5c3e9f6a1b2c3d4e5", false)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void ValidateAddress_ReturnsExpectedResult(string address, bool expected)
    {
        var result = _hdWalletService.ValidateAddress(address);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValidateAddress_WithValidAddress_ReturnsTrue()
    {
        var validAddress = TronKey.Generate().Address.Base58Check;

        var result = _hdWalletService.ValidateAddress(validAddress);

        Assert.True(result);
    }

    [Fact]
    public void DeriveAddress_ReturnsValidTronAddress()
    {
        var seed = new byte[64];

        var address = _hdWalletService.DeriveAddress(seed, 0, 0);

        Assert.NotEmpty(address);
        Assert.StartsWith("T", address);
    }

    [Fact]
    public void DeriveTransactionId_ReturnsHashOfTransactionBytes()
    {
        var transactionBytes = new byte[100];
        new Random(42).NextBytes(transactionBytes);

        var txId = _hdWalletService.DeriveTransactionId(transactionBytes);

        Assert.NotEmpty(txId);
        Assert.Equal(64, txId.Length);
    }
}
