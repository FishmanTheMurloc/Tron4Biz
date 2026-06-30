using System.Security.Cryptography;
using Tron4Biz.Crypto;

namespace Tron4Biz;

public class HDWalletService : IHDWalletService
{
    public string GenerateMnemonic()
    {
        var mnemonic = Mnemonic.Generate(12);
        return mnemonic.Sentence;
    }

    public byte[] DeriveSeed(string mnemonic)
    {
        var words = mnemonic.Split(' ');
        var mn = new Mnemonic(words);
        return mn.ToSeed();
    }

    public bool ValidateAddress(string address)
    {
        return TronAddressUtils.IsValid(address);
    }

    public string DeriveAddress(byte[] seed, int index, int account)
    {
        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account, 0, index);
        return derived.Address;
    }

    public byte[] DerivePrivateKey(byte[] seed, int index, int account)
    {
        var wallet = HDWallet.FromSeed(seed);
        var derived = wallet.DeriveAddress(account, 0, index);
        return Convert.FromHexString(derived.PrivateKey);
    }

    public string DeriveTransactionId(byte[] transactionBytes)
    {
        var txHash = SHA256.HashData(transactionBytes);
        return Convert.ToHexString(txHash).ToLowerInvariant();
    }
}
