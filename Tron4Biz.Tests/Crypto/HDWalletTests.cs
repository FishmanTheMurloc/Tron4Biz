using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class HDWalletTests
{
    [Fact]
    public void FromMnemonic_ValidMnemonic_CreatesHDWallet()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var wallet = HDWallet.FromMnemonic(mnemonic, "alice");

        Assert.NotNull(wallet);
        Assert.NotNull(wallet.MasterKey);
        Assert.Equal(32, wallet.MasterKey.Length);
    }

    [Fact]
    public void FromSeed_ValidSeed_CreatesHDWallet()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);

        Assert.NotNull(wallet);
        Assert.NotNull(wallet.MasterKey);
        Assert.Equal(32, wallet.MasterKey.Length);
    }

    [Fact]
    public void FromSeed_EmptySeed_ThrowsException()
    {
        var emptySeed = Array.Empty<byte>();
        Assert.Throws<ArgumentException>(() => HDWallet.FromSeed(emptySeed));
    }

    [Fact]
    public void FromSeed_NullSeed_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => HDWallet.FromSeed((byte[])null!));
    }

    [Fact]
    public void FromMnemonic_EmptyMnemonic_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => HDWallet.FromMnemonic(""));
        Assert.Throws<ArgumentException>(() => HDWallet.FromMnemonic("   "));
    }

    [Fact]
    public void FromMnemonic_SameInputs_SameMasterKey()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var wallet1 = HDWallet.FromMnemonic(mnemonic, "alice");
        var wallet2 = HDWallet.FromMnemonic(mnemonic, "alice");

        Assert.Equal(wallet1.MasterKey, wallet2.MasterKey);
    }

    [Fact]
    public void FromMnemonic_DifferentPassphrase_DifferentMasterKey()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var wallet1 = HDWallet.FromMnemonic(mnemonic, "alice");
        var wallet2 = HDWallet.FromMnemonic(mnemonic, "bob");

        Assert.NotEqual(wallet1.MasterKey, wallet2.MasterKey);
    }

    [Fact]
    public void FromSeed_ValidSeed_ProducesDeterministicResult()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet1 = HDWallet.FromSeed(seed);
        var wallet2 = HDWallet.FromSeed(seed);

        Assert.Equal(wallet1.MasterKey, wallet2.MasterKey);
    }
}
