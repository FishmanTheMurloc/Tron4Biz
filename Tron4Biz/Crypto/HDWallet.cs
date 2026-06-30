using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;

namespace Tron4Biz.Crypto;

public class HDWallet
{
    private static readonly ECDomainParameters DomainParams;

    static HDWallet()
    {
        var curveParams = ECNamedCurveTable.GetByName("secp256k1");
        DomainParams = new ECDomainParameters(
            curveParams.Curve,
            curveParams.G,
            curveParams.N,
            curveParams.H,
            curveParams.GetSeed()
        );
    }

    private readonly byte[] _privateKey;
    private readonly byte[] _chainCode;

    public byte[] MasterKey => _privateKey.ToArray();
    public byte[] ChainCode => _chainCode.ToArray();

    private HDWallet(byte[] privateKey, byte[] chainCode)
    {
        _privateKey = privateKey;
        _chainCode = chainCode;
    }

    public static HDWallet FromMnemonic(string mnemonic, string passphrase = "")
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
            throw new ArgumentException("Mnemonic cannot be empty", nameof(mnemonic));

        Mnemonic mn = Mnemonic.FromSentence(mnemonic);
        byte[] seed = mn.ToSeed(passphrase);
        var masterKey = ComputeMasterKey(seed);

        return new HDWallet(masterKey.PrivateKey, masterKey.ChainCode);
    }

    public static HDWallet FromSeed(byte[] seed)
    {
        if (seed == null || seed.Length == 0)
            throw new ArgumentException("Seed cannot be empty", nameof(seed));

        var masterKey = ComputeMasterKey(seed);
        return new HDWallet(masterKey.PrivateKey, masterKey.ChainCode);
    }

    private static (byte[] PrivateKey, byte[] ChainCode) ComputeMasterKey(byte[] seed)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes("Bitcoin seed"));
        byte[] result = hmac.ComputeHash(seed);

        byte[] privateKey = new byte[32];
        byte[] chainCode = new byte[32];
        Array.Copy(result, 0, privateKey, 0, 32);
        Array.Copy(result, 32, chainCode, 0, 32);

        return (privateKey, chainCode);
    }

    public DerivedAddress DeriveAddress(int account = 0, int change = 0, int index = 0)
    {
        string path = $"m/44'/195'/{account}'/{change}/{index}";

        var derivedKey = DerivePath(_privateKey, _chainCode, path);
        string address = TronAddressUtils.FromPrivateKey(derivedKey.PrivateKey);
        string hexPrivateKey = Convert.ToHexString(derivedKey.PrivateKey).ToLowerInvariant();

        byte[] publicKey = GetPublicKey(derivedKey.PrivateKey);

        return new DerivedAddress
        {
            Address = address,
            PrivateKey = hexPrivateKey,
            Path = path,
            Index = index,
            PublicKey = Convert.ToHexString(publicKey).ToLowerInvariant()
        };
    }

    private static (byte[] PrivateKey, byte[] ChainCode) DerivePath(byte[] privateKey, byte[] chainCode, string path)
    {
        var parts = path.Split('/').Skip(1).ToArray();
        byte[] key = privateKey;
        byte[] code = chainCode;

        foreach (var part in parts)
        {
            bool hardened = part.EndsWith("'");
            int index = int.Parse(hardened ? part[..^1] : part);

            var derived = DeriveChildKey(key, code, index, hardened);
            key = derived.PrivateKey;
            code = derived.ChainCode;
        }

        return (key, code);
    }

    private static (byte[] PrivateKey, byte[] ChainCode) DeriveChildKey(byte[] parentPrivateKey, byte[] parentChainCode, int index, bool hardened)
    {
        byte[] data;

        if (hardened)
        {
            data = new byte[37];
            data[0] = 0x00;
            Array.Copy(parentPrivateKey, 0, data, 1, 32);
        }
        else
        {
            byte[] publicKey = GetPublicKey(parentPrivateKey);
            data = new byte[37];
            // 使用压缩公钥：0x02 (偶数Y) 或 0x03 (奇数Y) + X坐标(32字节)
            data[0] = (byte)(0x02 + (publicKey[64] & 0x01));
            Array.Copy(publicKey, 1, data, 1, 32);  // 跳过0x04前缀，复制X坐标
        }

        uint indexUint = hardened ? ((uint)index | 0x80000000) : (uint)index;
        byte[] indexBytes = BitConverter.GetBytes(indexUint);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(indexBytes);
        Array.Copy(indexBytes, 0, data, 33, 4);

        using var hmac = new HMACSHA512(parentChainCode);
        byte[] result = hmac.ComputeHash(data);

        byte[] il = new byte[32];
        byte[] ir = new byte[32];
        Array.Copy(result, 0, il, 0, 32);
        Array.Copy(result, 32, ir, 0, 32);

        var ilBigInt = new Org.BouncyCastle.Math.BigInteger(1, il);
        var parentKeyBigInt = new Org.BouncyCastle.Math.BigInteger(1, parentPrivateKey);
        var curveOrder = DomainParams.N;

        var childKeyInt = ilBigInt.Add(parentKeyBigInt).Mod(curveOrder);

        if (childKeyInt.Equals(Org.BouncyCastle.Math.BigInteger.Zero))
            throw new Exception("Invalid child key");

        byte[] childPrivateKey = childKeyInt.ToByteArrayUnsigned();

        return (childPrivateKey, ir);
    }

    private static byte[] GetPublicKey(byte[] privateKey)
    {
        var keyParams = new ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(1, privateKey), DomainParams);
        return keyParams.Parameters.G.Multiply(keyParams.D).GetEncoded(false);
    }
}

public class DerivedAddress
{
    public string Address { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Index { get; set; }
    public string PublicKey { get; set; } = string.Empty;
}
