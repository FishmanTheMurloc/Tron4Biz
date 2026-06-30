using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class HDWalletDerivationTests
{
    [Fact]
    public void DeriveAddress_DifferentIndex_ReturnsDifferentAddresses()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);

        var addr0 = wallet.DeriveAddress(account: 0, index: 0);
        var addr1 = wallet.DeriveAddress(account: 0, index: 1);

        Assert.NotEqual(addr0.Address, addr1.Address);
        Assert.Equal(0, addr0.Index);
        Assert.Equal(1, addr1.Index);
        Assert.Equal("m/44'/195'/0'/0/0", addr0.Path);
        Assert.Equal("m/44'/195'/0'/0/1", addr1.Path);
    }

    [Fact]
    public void DeriveAddress_DifferentAccount_ReturnsDifferentAddresses()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);

        var addrAccount0 = wallet.DeriveAddress(account: 0, index: 0);
        var addrAccount1 = wallet.DeriveAddress(account: 1, index: 0);

        Assert.NotEqual(addrAccount0.Address, addrAccount1.Address);
        Assert.Equal(0, addrAccount0.Index);
        Assert.Equal(0, addrAccount1.Index);
        Assert.Equal("m/44'/195'/0'/0/0", addrAccount0.Path);
        Assert.Equal("m/44'/195'/1'/0/0", addrAccount1.Path);
    }

    [Fact]
    public void DeriveAddress_SamePath_ReturnsSameAddress()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);

        var addr1 = wallet.DeriveAddress(account: 0, index: 5);
        var addr2 = wallet.DeriveAddress(account: 0, index: 5);

        Assert.Equal(addr1.Address, addr2.Address);
        Assert.Equal(addr1.PrivateKey, addr2.PrivateKey);
        Assert.Equal(addr1.PublicKey, addr2.PublicKey);
    }

    [Fact]
    public void DeriveAddress_MultipleIndices_AllUnique()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);

        var addresses = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            var derived = wallet.DeriveAddress(account: 0, index: i);
            Assert.True(addresses.Add(derived.Address), $"Duplicate address at index {i}");
        }

        Assert.Equal(10, addresses.Count);
    }

    [Fact]
    public void DeriveAddress_DifferentSeeds_DifferentAddresses()
    {
        var seed1 = new byte[64];
        for (int i = 0; i < seed1.Length; i++) seed1[i] = (byte)i;

        var seed2 = new byte[64];
        for (int i = 0; i < seed2.Length; i++) seed2[i] = (byte)(i + 1);

        var wallet1 = HDWallet.FromSeed(seed1);
        var wallet2 = HDWallet.FromSeed(seed2);

        var addr1 = wallet1.DeriveAddress(account: 0, index: 0);
        var addr2 = wallet2.DeriveAddress(account: 0, index: 0);

        Assert.NotEqual(addr1.Address, addr2.Address);
    }

    [Fact]
    public void DeriveAddress_ValidTronAddressFormat()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account: 0, index: 0);

        Assert.NotNull(derived.Address);
        Assert.StartsWith("T", derived.Address);
        Assert.Equal(34, derived.Address.Length);

        Assert.NotNull(derived.PrivateKey);
        Assert.Equal(64, derived.PrivateKey.Length);

        Assert.NotNull(derived.PublicKey);
        Assert.Equal(130, derived.PublicKey.Length);  // 未压缩公钥: 65字节 = 130字符十六进制
    }

    [Fact]
    public void DeriveAddress_SignAndVerify_Success()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account: 0, index: 0);

        var key = TronKey.FromPrivateKey(derived.PrivateKey);

        var messageHash = new byte[32];
        var signature = key.Sign(messageHash);

        var isValid = TronKey.Verify(messageHash, signature, key.PublicKeyBytes);

        Assert.True(isValid);
    }

    [Fact]
    public void DeriveAddress_SignAndRecover_Success()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account: 0, index: 0);

        var key = TronKey.FromPrivateKey(derived.PrivateKey);
        var messageHash = new byte[32];
        var signature = key.Sign(messageHash);

        var recoveredKey = TronKey.RecoverFromSignature(messageHash, signature);

        Assert.NotNull(recoveredKey);
        Assert.Equal(key.PublicKeyHex, recoveredKey.PublicKeyHex);
    }

    [Fact]
    public void DeriveAddress_PublicKeyMatchesDerived()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account: 0, index: 0);

        var key = TronKey.FromPrivateKey(derived.PrivateKey);

        Assert.Equal(derived.PublicKey, key.PublicKeyHex);
    }

    [Fact]
    public void DeriveAddress_AddressMatchesTronKey()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account: 0, index: 0);

        var key = TronKey.FromPrivateKey(derived.PrivateKey);

        Assert.Equal(derived.Address, key.Address.ToString());
    }

    [Fact]
    public void DeriveAddress_DifferentIndices_SignAndVerify_AllValid()
    {
        var seed = new byte[64];
        for (int i = 0; i < seed.Length; i++) seed[i] = (byte)i;

        var wallet = HDWallet.FromSeed(seed);
        var messageHash = new byte[32];

        for (int i = 0; i < 5; i++)
        {
            var derived = wallet.DeriveAddress(account: 0, index: i);
            var key = TronKey.FromPrivateKey(derived.PrivateKey);
            var signature = key.Sign(messageHash);
            var isValid = TronKey.Verify(messageHash, signature, key.PublicKeyBytes);

            Assert.True(isValid, $"Signature verification failed for index {i}");
        }
    }
}
