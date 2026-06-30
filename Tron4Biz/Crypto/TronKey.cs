using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

namespace Tron4Biz.Crypto;

public class TronKey
{
    private static readonly ECDomainParameters DomainParams;
    private static readonly BigInteger HALF_CURVE_ORDER;
    private static readonly BigInteger CURVE_N;

    static TronKey()
    {
        var curveParams = ECNamedCurveTable.GetByName("secp256k1");
        DomainParams = new ECDomainParameters(
            curveParams.Curve,
            curveParams.G,
            curveParams.N,
            curveParams.H,
            curveParams.GetSeed()
        );
        CURVE_N = curveParams.N;
        HALF_CURVE_ORDER = CURVE_N.ShiftRight(1);
    }

    private readonly ECPrivateKeyParameters? _privateKeyParams;
    private readonly ECPublicKeyParameters _publicKeyParams;
    private byte[]? _addressBytes;
    private byte[]? _nodeId;

    public byte[] PrivateKeyBytes => _privateKeyParams == null ? Array.Empty<byte>() : _privateKeyParams.D.ToByteArrayUnsigned();
    public byte[] PublicKeyBytes => _publicKeyParams.Q.GetEncoded(false);
    public byte[] CompressedPublicKeyBytes => _publicKeyParams.Q.GetEncoded(true);

    public TronAddress Address => TronAddress.FromPrivateKey(PrivateKeyBytes);

    public string PrivateKeyHex => Convert.ToHexString(PrivateKeyBytes).ToLowerInvariant();
    public string PublicKeyHex => Convert.ToHexString(PublicKeyBytes).ToLowerInvariant();

    public static TronKey Generate()
    {
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(DomainParams, new SecureRandom()));
        var keyPair = generator.GenerateKeyPair();
        return new TronKey((ECPrivateKeyParameters)keyPair.Private, (ECPublicKeyParameters)keyPair.Public);
    }

    public static TronKey FromPrivateKey(byte[] privateKey)
    {
        if (privateKey == null || privateKey.Length != 32)
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));

        var d = new BigInteger(1, privateKey);
        var privateKeyParams = new ECPrivateKeyParameters(d, DomainParams);
        var publicKeyParams = new ECPublicKeyParameters(DomainParams.G.Multiply(d), DomainParams);

        return new TronKey(privateKeyParams, publicKeyParams);
    }

    public static TronKey FromPrivateKey(string hexPrivateKey)
    {
        return FromPrivateKey(Convert.FromHexString(hexPrivateKey));
    }

    public static TronKey FromPublicKey(byte[] publicKey)
    {
        if (publicKey == null || (publicKey.Length != 33 && publicKey.Length != 65))
            throw new ArgumentException("Public key must be 33 or 65 bytes", nameof(publicKey));

        var q = DomainParams.Curve.DecodePoint(publicKey);
        var publicKeyParams = new ECPublicKeyParameters(q, DomainParams);

        return new TronKey(null, publicKeyParams);
    }

    private TronKey(ECPrivateKeyParameters? privateKeyParams, ECPublicKeyParameters? publicKeyParams)
    {
        _privateKeyParams = privateKeyParams;
        if (privateKeyParams != null)
        {
            _publicKeyParams = new ECPublicKeyParameters(DomainParams.G.Multiply(privateKeyParams.D), DomainParams);
        }
        else if (publicKeyParams != null)
        {
            _publicKeyParams = publicKeyParams;
        }
        else
        {
            throw new ArgumentException("Either private key or public key must be provided");
        }
    }

    public TronSignature Sign(byte[] messageHash)
    {
        if (_privateKeyParams == null)
            throw new InvalidOperationException("Cannot sign without private key");

        if (messageHash.Length != 32)
            throw new ArgumentException("Message hash must be 32 bytes", nameof(messageHash));

        var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, _privateKeyParams);

        var signature = signer.GenerateSignature(messageHash);

        var r = signature[0];
        var s = signature[1];

        if (s.CompareTo(HALF_CURVE_ORDER) > 0)
        {
            s = CURVE_N.Subtract(s);
        }

        int recId = FindRecoveryId(r, s, messageHash);

        return new TronSignature(r, s, (byte)(recId + 27));
    }

    public static bool Verify(byte[] messageHash, TronSignature signature, byte[] publicKey)
    {
        if (messageHash.Length != 32)
            throw new ArgumentException("Message hash must be 32 bytes", nameof(messageHash));

        if (publicKey.Length != 33 && publicKey.Length != 65)
            throw new ArgumentException("Public key must be 33 or 65 bytes", nameof(publicKey));

        try
        {
            var q = DomainParams.Curve.DecodePoint(publicKey);
            var publicKeyParams = new ECPublicKeyParameters(q, DomainParams);

            var signer = new ECDsaSigner();
            signer.Init(false, publicKeyParams);

            return signer.VerifySignature(messageHash, signature.R, signature.S);
        }
        catch
        {
            return false;
        }
    }

    public static byte[]? RecoverPublicKey(byte[] messageHash, TronSignature signature)
    {
        int recId = signature.V - 27;
        if (recId < 0 || recId > 3)
            throw new ArgumentException("Invalid recovery id", nameof(signature));

        return RecoverPubBytesFromSignature(recId, signature.R, signature.S, messageHash);
    }

    public static TronKey? RecoverFromSignature(byte[] messageHash, TronSignature signature)
    {
        try
        {
            var pubBytes = RecoverPublicKey(messageHash, signature);
            if (pubBytes == null) return null;
            return FromPublicKey(pubBytes);
        }
        catch
        {
            return null;
        }
    }

    private int FindRecoveryId(BigInteger r, BigInteger s, byte[] messageHash)
    {
        var thisKeyBytes = _publicKeyParams.Q.GetEncoded(false);

        for (int i = 0; i < 4; i++)
        {
            var recoveredKey = RecoverPubBytesFromSignature(i, r, s, messageHash);
            if (recoveredKey != null && IsEqual(recoveredKey, thisKeyBytes))
            {
                return i;
            }
        }

        throw new InvalidOperationException("Could not find recovery id");
    }

    private static bool IsEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        bool result = true;
        for (int i = 0; i < a.Length; i++)
        {
            result &= (a[i] == b[i]);
        }
        return result;
    }

    private static byte[]? RecoverPubBytesFromSignature(int recId, BigInteger r, BigInteger s, byte[] messageHash)
    {
        var n = CURVE_N;
        var i = BigInteger.ValueOf(recId / 2);
        var x = r.Add(i.Multiply(n));

        if (x.CompareTo(n) >= 0)
            return null;

        var R = DecompressKey(x, (recId & 1) == 1);
        if (!R.Multiply(n).IsInfinity)
            return null;

        var e = new BigInteger(1, messageHash);
        var eInv = BigInteger.Zero.Subtract(e).Mod(n);
        var rInv = r.ModInverse(n);
        var srInv = rInv.Multiply(s).Mod(n);
        var eInvrInv = rInv.Multiply(eInv).Mod(n);

        var q = ECAlgorithms.SumOfTwoMultiplies(DomainParams.G, eInvrInv, R, srInv);
        return q.GetEncoded(false);
    }

    private static ECPoint DecompressKey(BigInteger xBN, bool yBit)
    {
        var curve = DomainParams.Curve;
        var enc = new byte[33];
        enc[0] = (byte)(yBit ? 0x03 : 0x02);
        var xBytes = xBN.ToByteArrayUnsigned();
        Array.Copy(xBytes, 0, enc, 33 - xBytes.Length, xBytes.Length);
        return curve.DecodePoint(enc);
    }

    public static bool IsPubKeyCanonical(byte[] pubkey)
    {
        if (pubkey[0] == 0x04)
            return pubkey.Length == 65;
        if (pubkey[0] == 0x02 || pubkey[0] == 0x03)
            return pubkey.Length == 33;
        return false;
    }

    public bool IsPubKeyCanonical()
    {
        return IsPubKeyCanonical(PublicKeyBytes);
    }

    public byte[] GetAddress()
    {
        if (_addressBytes == null)
        {
            _addressBytes = TronAddress.FromPrivateKey(PrivateKeyBytes).AddressBytes;
        }
        return _addressBytes;
    }

    public byte[] GetNodeId()
    {
        if (_nodeId == null)
        {
            var pubKey = PublicKeyBytes;
            _nodeId = new byte[64];
            Array.Copy(pubKey, 1, _nodeId, 0, 64);
        }
        return _nodeId;
    }
}

public struct TronSignature
{
    private static readonly BigInteger SECP256K1N = new BigInteger("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", 16);

    public BigInteger R { get; }
    public BigInteger S { get; }
    public byte V { get; }

    public TronSignature(BigInteger r, BigInteger s, byte v)
    {
        R = r;
        S = s;
        V = v;
    }

    public static TronSignature FromComponents(byte[] r, byte[] s, byte v)
    {
        return new TronSignature(
            new BigInteger(1, r),
            new BigInteger(1, s),
            v);
    }

    public static bool ValidateComponents(BigInteger r, BigInteger s, byte v)
    {
        if (v != 27 && v != 28)
            return false;
        if (r.CompareTo(BigInteger.One) < 0 || s.CompareTo(BigInteger.One) < 0)
            return false;

        if (r.CompareTo(SECP256K1N) >= 0 || s.CompareTo(SECP256K1N) >= 0)
            return false;

        return true;
    }

    public bool ValidateComponents()
    {
        return ValidateComponents(R, S, V);
    }

    public byte[] ToByteArray()
    {
        var fixedV = V >= 27 ? (byte)(V - 27) : V;
        var rBytes = R.ToByteArrayUnsigned();
        var sBytes = S.ToByteArrayUnsigned();

        var result = new byte[65];
        Array.Copy(rBytes, 0, result, 32 - rBytes.Length, rBytes.Length);
        Array.Copy(sBytes, 0, result, 64 - sBytes.Length, sBytes.Length);
        result[64] = fixedV;

        return result;
    }

    public string ToHex()
    {
        return Convert.ToHexString(ToByteArray()).ToLowerInvariant();
    }

    public static TronSignature FromHex(string signatureHex)
    {
        var bytes = Convert.FromHexString(signatureHex);
        if (bytes.Length != 65)
            throw new ArgumentException("Signature must be 65 bytes");

        var r = new byte[32];
        var s = new byte[32];
        var v = bytes[64];

        Array.Copy(bytes, 0, r, 0, 32);
        Array.Copy(bytes, 32, s, 0, 32);

        return FromComponents(r, s, (byte)(v + 27));
    }
}
