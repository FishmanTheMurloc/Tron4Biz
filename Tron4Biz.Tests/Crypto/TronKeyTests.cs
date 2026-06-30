using System.Security.Cryptography;
using Org.BouncyCastle.Math;
using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class TronKeyTests
{
    private static readonly byte[] TestPrivateKey = Convert.FromHexString("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");

    [Fact]
    public void Generate_CreatesValidKey()
    {
        var key = TronKey.Generate();

        Assert.NotNull(key);
        Assert.NotNull(key.PrivateKeyBytes);
        Assert.Equal(32, key.PrivateKeyBytes.Length);
        Assert.NotNull(key.PublicKeyBytes);
        Assert.Equal(65, key.PublicKeyBytes.Length);
        Assert.True(key.IsPubKeyCanonical());
    }

    [Fact]
    public void FromPrivateKey_ValidBytes_CreatesKey()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        Assert.NotNull(key);
        Assert.Equal(32, key.PrivateKeyBytes.Length);
        Assert.Equal(Convert.ToHexString(TestPrivateKey).ToLowerInvariant(), key.PrivateKeyHex);
    }

    [Fact]
    public void FromPrivateKey_InvalidLength_ThrowsException()
    {
        var shortKey = new byte[31];
        Assert.Throws<ArgumentException>(() => TronKey.FromPrivateKey(shortKey));
    }

    [Fact]
    public void FromPrivateKey_Null_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => TronKey.FromPrivateKey((byte[])null!));
    }

    [Fact]
    public void FromPrivateKey_Empty_ThrowsException()
    {
        var emptyKey = Array.Empty<byte>();
        Assert.Throws<ArgumentException>(() => TronKey.FromPrivateKey(emptyKey));
    }

    [Fact]
    public void FromPublicKey_ValidKey_CreatesWatchingKey()
    {
        var keyFromPriv = TronKey.FromPrivateKey(TestPrivateKey);
        var keyFromPub = TronKey.FromPublicKey(keyFromPriv.PublicKeyBytes);

        Assert.NotNull(keyFromPub);
        Assert.Equal(keyFromPriv.PublicKeyHex, keyFromPub.PublicKeyHex);
    }

    [Fact]
    public void FromPublicKey_InvalidLength_ThrowsException()
    {
        var invalidPubKey = new byte[64];
        Assert.Throws<ArgumentException>(() => TronKey.FromPublicKey(invalidPubKey));
    }

    [Fact]
    public void PublicKeyBytes_ReturnsCorrectFormat()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        Assert.Equal(65, key.PublicKeyBytes.Length);
        Assert.Equal(0x04, key.PublicKeyBytes[0]);
    }

    [Fact]
    public void CompressedPublicKeyBytes_ReturnsCorrectFormat()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        Assert.Equal(33, key.CompressedPublicKeyBytes.Length);
        Assert.True(key.CompressedPublicKeyBytes[0] == 0x02 || key.CompressedPublicKeyBytes[0] == 0x03);
    }

    [Fact]
    public void FromPrivateKey_ThenFromPublicKey_ProduceSamePublicKey()
    {
        var keyFromPriv = TronKey.FromPrivateKey(TestPrivateKey);
        var keyFromPub = TronKey.FromPublicKey(keyFromPriv.PublicKeyBytes);

        Assert.Equal(keyFromPriv.PublicKeyHex, keyFromPub.PublicKeyHex);
    }

    [Fact]
    public void Sign_ValidMessage_ReturnsSignature()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];

        var signature = key.Sign(messageHash);

        Assert.True(signature.ValidateComponents());
    }

    [Fact]
    public void Sign_InvalidMessageLength_ThrowsException()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var invalidMessage = new byte[31];

        Assert.Throws<ArgumentException>(() => key.Sign(invalidMessage));
    }

    [Fact]
    public void Sign_SignatureFormat_Is65Bytes()
    {
        var key = TronKey.Generate();
        var messageHash = new byte[32];

        var signature = key.Sign(messageHash);
        var signatureBytes = signature.ToByteArray();

        Assert.Equal(65, signatureBytes.Length);
    }

    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];
        var signature = key.Sign(messageHash);

        var isValid = TronKey.Verify(messageHash, signature, key.PublicKeyBytes);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_WrongPublicKey_ReturnsFalse()
    {
        var key1 = TronKey.FromPrivateKey(TestPrivateKey);
        var key2 = TronKey.Generate();
        var messageHash = new byte[32];
        var signature = key1.Sign(messageHash);

        var isValid = TronKey.Verify(messageHash, signature, key2.PublicKeyBytes);

        Assert.False(isValid);
    }

    [Fact]
    public void Verify_TamperedMessage_ReturnsFalse()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];
        var signature = key.Sign(messageHash);

        var tamperedMessage = new byte[32];
        tamperedMessage[0] = 0x01;

        var isValid = TronKey.Verify(tamperedMessage, signature, key.PublicKeyBytes);

        Assert.False(isValid);
    }

    [Fact]
    public void RecoverFromSignature_ValidSignature_ReturnsKey()
    {
        var originalKey = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];
        var signature = originalKey.Sign(messageHash);

        var recoveredKey = TronKey.RecoverFromSignature(messageHash, signature);

        Assert.NotNull(recoveredKey);
        Assert.Equal(originalKey.PublicKeyHex, recoveredKey.PublicKeyHex);
    }

    [Fact]
    public void RecoverPublicKey_ValidSignature_ReturnsPublicKey()
    {
        var originalKey = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];
        var signature = originalKey.Sign(messageHash);

        var recoveredPubKey = TronKey.RecoverPublicKey(messageHash, signature);

        Assert.NotNull(recoveredPubKey);
        Assert.Equal(originalKey.PublicKeyBytes, recoveredPubKey);
    }

    [Fact]
    public void GetAddress_ReturnsValidTronAddress()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        var address = key.Address;

        Assert.NotNull(address);
        Assert.StartsWith("T", address.Base58Check);
        Assert.Equal(21, address.AddressBytes.Length);
        Assert.Equal(0x41, address.AddressBytes[0]);
    }

    [Fact]
    public void GetNodeId_Returns64Bytes()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        var nodeId = key.GetNodeId();

        Assert.NotNull(nodeId);
        Assert.Equal(64, nodeId.Length);
    }

    [Fact]
    public void IsPubKeyCanonical_ValidUncompressedKey_ReturnsTrue()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        Assert.True(TronKey.IsPubKeyCanonical(key.PublicKeyBytes));
    }

    [Fact]
    public void IsPubKeyCanonical_ValidCompressedKey_ReturnsTrue()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);

        Assert.True(TronKey.IsPubKeyCanonical(key.CompressedPublicKeyBytes));
    }

    [Fact]
    public void IsPubKeyCanonical_WrongLength_ReturnsFalse()
    {
        var wrongLengthKey = new byte[64];
        wrongLengthKey[0] = 0x04;

        Assert.False(TronKey.IsPubKeyCanonical(wrongLengthKey));
    }

    [Fact]
    public void IsPubKeyCanonical_WrongPrefix_ReturnsFalse()
    {
        var wrongPrefixKey = new byte[65];
        wrongPrefixKey[0] = 0x05;

        Assert.False(TronKey.IsPubKeyCanonical(wrongPrefixKey));
    }

    [Fact]
    public void TronSignature_ValidateComponents_ValidSignature_ReturnsTrue()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];
        var signature = key.Sign(messageHash);

        Assert.True(signature.ValidateComponents());
    }

    [Fact]
    public void TronSignature_ValidateComponents_InvalidV_ReturnsFalse()
    {
        var signature = new TronSignature(BigInteger.One, BigInteger.One, 30);

        Assert.False(signature.ValidateComponents());
    }

    [Fact]
    public void TronSignature_FromHex_RoundTrips()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];
        var originalSignature = key.Sign(messageHash);
        var signatureHex = originalSignature.ToHex();

        var recoveredSignature = TronSignature.FromHex(signatureHex);

        Assert.Equal(originalSignature.R, recoveredSignature.R);
        Assert.Equal(originalSignature.S, recoveredSignature.S);
        Assert.Equal(originalSignature.V, recoveredSignature.V);
    }

    [Fact]
    public void Sign_AndRecover_RoundTrips()
    {
        var key = TronKey.Generate();
        var messageHash = new byte[32];
        RandomNumberGenerator.Fill(messageHash);

        var signature = key.Sign(messageHash);
        var recoveredKey = TronKey.RecoverFromSignature(messageHash, signature);

        Assert.NotNull(recoveredKey);
        Assert.Equal(key.PublicKeyHex, recoveredKey.PublicKeyHex);
    }

    [Fact]
    public void Sign_AndVerify_RoundTrips()
    {
        var key = TronKey.Generate();
        var messageHash = new byte[32];
        RandomNumberGenerator.Fill(messageHash);

        var signature = key.Sign(messageHash);
        var isValid = TronKey.Verify(messageHash, signature, key.PublicKeyBytes);

        Assert.True(isValid);
    }

    [Fact]
    public void Sign_SameKeySameMessage_ProducesDeterministicSignature()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var messageHash = new byte[32];

        var sig1 = key.Sign(messageHash);
        var sig2 = key.Sign(messageHash);

        Assert.Equal(sig1.R, sig2.R);
        Assert.Equal(sig1.S, sig2.S);
    }

    [Fact]
    public void Sign_DifferentKeys_SameMessage_BothRecoverable()
    {
        var key1 = TronKey.Generate();
        var key2 = TronKey.Generate();
        var messageHash = new byte[32];
        RandomNumberGenerator.Fill(messageHash);

        var sig1 = key1.Sign(messageHash);
        var sig2 = key2.Sign(messageHash);

        var recovered1 = TronKey.RecoverFromSignature(messageHash, sig1);
        var recovered2 = TronKey.RecoverFromSignature(messageHash, sig2);

        Assert.NotNull(recovered1);
        Assert.NotNull(recovered2);
        Assert.Equal(key1.PublicKeyHex, recovered1.PublicKeyHex);
        Assert.Equal(key2.PublicKeyHex, recovered2.PublicKeyHex);
    }

    [Fact]
    public void FromPublicKey_WatchingKey_CannotSign()
    {
        var key = TronKey.FromPrivateKey(TestPrivateKey);
        var watchingKey = TronKey.FromPublicKey(key.PublicKeyBytes);
        var messageHash = new byte[32];

        Assert.Throws<InvalidOperationException>(() => watchingKey.Sign(messageHash));
    }
}
