namespace Tron4Biz;

public interface IHDWalletService
{
    string GenerateMnemonic();
    byte[] DeriveSeed(string mnemonic);
    bool ValidateAddress(string address);
    string DeriveAddress(byte[] seed, int index, int account);
    byte[] DerivePrivateKey(byte[] seed, int index, int account);
    string DeriveTransactionId(byte[] transactionBytes);
}
