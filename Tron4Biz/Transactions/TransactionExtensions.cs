using System.Security.Cryptography;
using Google.Protobuf;
using Protocol;
using Tron4Biz.Crypto;

namespace Tron4Biz.Transactions;

public static class TransactionExtensions
{
    /// <summary>
    /// 使用指定的私钥对交易进行签名，并返回已签名的交易副本。
    /// </summary>
    /// <param name="transaction">待签名的交易。</param>
    /// <param name="key">用于签名的私钥。</param>
    /// <returns>包含签名的交易副本。</returns>
    public static Transaction SignTransaction(this Transaction transaction, TronKey key)
    {
        var signature = key.Sign(SHA256.HashData(transaction.RawData.ToByteArray()));
        return transaction.SignTransaction(signature.ToByteArray());
    }

    /// <summary>
    /// 将外部签名字节附加到交易中，并返回已签名的交易副本。
    /// </summary>
    /// <param name="transaction">待签名的交易。</param>
    /// <param name="signatureBytes">外部签名结果，通常为 65 字节的 ECDSA 签名。</param>
    /// <returns>包含签名的交易副本。</returns>
    public static Transaction SignTransaction(this Transaction transaction, byte[] signatureBytes)
    {
        var signed = transaction.Clone();
        signed.Signature.Add(ByteString.CopyFrom(signatureBytes));
        return signed;
    }
}
