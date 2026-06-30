using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;

namespace Tron4Biz.Crypto;

public class TronAddress
{
    public const byte MainnetPrefix = 0x41;
    public const int AddressLength = 21;

    private static readonly ECDomainParameters DomainParams;

    static TronAddress()
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

    private readonly byte[] _addressBytes;

    public byte[] AddressBytes => _addressBytes.ToArray();

    public string Base58Check => EncodeBase58Check(_addressBytes);

    public string Hex => Convert.ToHexString(_addressBytes).ToLowerInvariant();

    private TronAddress(byte[] addressBytes)
    {
        if (addressBytes.Length != AddressLength)
            throw new ArgumentException($"Address must be {AddressLength} bytes", nameof(addressBytes));
        _addressBytes = addressBytes;
    }

    public static TronAddress FromPrivateKey(byte[] privateKey)
    {
        if (privateKey == null || privateKey.Length != 32)
            throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));

        byte[] publicKey = GetPublicKeyFromPrivate(privateKey);
        byte[] addressBytes = ComputeAddress(publicKey);

        return new TronAddress(addressBytes);
    }

    public static TronAddress FromPrivateKey(string hexPrivateKey)
    {
        return FromPrivateKey(Convert.FromHexString(hexPrivateKey));
    }

    public static TronAddress FromBase58(string base58Address)
    {
        byte[] decoded = DecodeBase58Check(base58Address);
        return new TronAddress(decoded);
    }

    public static TronAddress FromHex(string hexAddress)
    {
        byte[] bytes = Convert.FromHexString(hexAddress);
        return new TronAddress(bytes);
    }

    public static TronAddress FromHexPrefix(string hexAddress)
    {
        string hex = hexAddress.StartsWith("0x") ? hexAddress[2..] : hexAddress;
        return FromHex(hex);
    }

    public static bool IsValid(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        try
        {
            if (address.StartsWith("T"))
            {
                var decoded = DecodeBase58Check(address);
                return decoded.Length == AddressLength && decoded[0] == MainnetPrefix;
            }

            if (address.StartsWith("0x") || address.Length == AddressLength * 2)
            {
                var bytes = Convert.FromHexString(address.StartsWith("0x") ? address[2..] : address);
                return bytes.Length == AddressLength && bytes[0] == MainnetPrefix;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GetPublicKeyFromPrivate(byte[] privateKey)
    {
        var keyParams = new ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(1, privateKey), DomainParams);
        return keyParams.Parameters.G.Multiply(keyParams.D).GetEncoded(false);
    }

    private static byte[] ComputeAddress(byte[] publicKey)
    {
        byte[] keccakHash = Keccak256(publicKey, 1, publicKey.Length - 1);
        byte[] addressBytes = new byte[AddressLength];
        addressBytes[0] = MainnetPrefix;
        Array.Copy(keccakHash, keccakHash.Length - 20, addressBytes, 1, 20);
        return addressBytes;
    }

    private static byte[] Keccak256(byte[] data, int offset, int length)
    {
        var digest = new KeccakDigest(256);
        byte[] result = new byte[digest.GetDigestSize()];
        digest.BlockUpdate(data, offset, length);
        digest.DoFinal(result, 0);
        return result;
    }

    private static string EncodeBase58Check(byte[] payload)
    {
        byte[] checksum = SHA256.HashData(SHA256.HashData(payload))[..4];
        byte[] fullBytes = new byte[payload.Length + 4];
        Array.Copy(payload, 0, fullBytes, 0, payload.Length);
        Array.Copy(checksum, 0, fullBytes, payload.Length, 4);

        return Base58.Encode(fullBytes);
    }

    private static byte[] DecodeBase58Check(string address)
    {
        byte[] decoded = Base58.Decode(address);

        if (decoded.Length < 4)
            throw new ArgumentException("Invalid address");

        byte[] payload = decoded[..^4];
        byte[] checksum = decoded[^4..];

        byte[] expectedChecksum = SHA256.HashData(SHA256.HashData(payload))[..4];

        if (!checksum.SequenceEqual(expectedChecksum))
            throw new ArgumentException("Invalid checksum");

        return payload;
    }

    public static implicit operator string(TronAddress address) => address.Base58Check;

    public override string ToString() => Base58Check;

    public override bool Equals(object? obj) =>
        obj is TronAddress other && _addressBytes.SequenceEqual(other._addressBytes);

    public override int GetHashCode()
    {
        return BitConverter.ToInt32(_addressBytes, 0);
    }
}

public static class TronAddressUtils
{
    public static string FromPrivateKey(byte[] privateKey)
    {
        return TronAddress.FromPrivateKey(privateKey).Base58Check;
    }

    public static string FromPrivateKey(string hexPrivateKey)
    {
        return TronAddress.FromPrivateKey(hexPrivateKey).Base58Check;
    }

    public static bool IsValid(string address)
    {
        return TronAddress.IsValid(address);
    }
}
