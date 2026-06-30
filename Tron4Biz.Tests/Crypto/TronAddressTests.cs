using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class TronAddressTests
{
    [Fact]
    public void FromPrivateKey_ValidKey_ReturnsAddress()
    {
        var privateKeyHex = "25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32";
        var address = TronAddress.FromPrivateKey(privateKeyHex);

        Assert.NotNull(address);
        Assert.StartsWith("T", address.Base58Check);
        Assert.Equal(21, address.AddressBytes.Length);
        Assert.Equal(0x41, address.AddressBytes[0]);
    }

    [Fact]
    public void FromPrivateKey_InvalidLength_ThrowsException()
    {
        var shortKey = new byte[31];
        Assert.Throws<ArgumentException>(() => TronAddress.FromPrivateKey(shortKey));
    }

    [Fact]
    public void FromPrivateKey_Null_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => TronAddress.FromPrivateKey((byte[])null!));
    }

    [Fact]
    public void FromBase58_ValidAddress_RoundTrips()
    {
        var originalKey = "25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32";
        var address = TronAddress.FromPrivateKey(originalKey);
        var decoded = TronAddress.FromBase58(address.Base58Check);

        Assert.Equal(address.Base58Check, decoded.Base58Check);
        Assert.Equal(address.Hex, decoded.Hex);
    }

    [Theory]
    [InlineData("TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValid_Tests(string address, bool expected)
    {
        Assert.Equal(expected, TronAddress.IsValid(address));
    }

    [Fact]
    public void AddressBytes_ReturnsCorrectLength()
    {
        var address = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        Assert.Equal(21, address.AddressBytes.Length);
    }

    [Fact]
    public void AddressBytes_FirstByte_IsMainnetPrefix()
    {
        var address = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        Assert.Equal(0x41, address.AddressBytes[0]);
    }

    [Fact]
    public void Hex_ReturnsLowercaseHex()
    {
        var address = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        Assert.DoesNotContain("A", address.Hex);
        Assert.DoesNotContain("B", address.Hex);
        Assert.DoesNotContain("C", address.Hex);
        Assert.DoesNotContain("D", address.Hex);
        Assert.DoesNotContain("E", address.Hex);
        Assert.DoesNotContain("F", address.Hex);
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var address = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        string addressString = address;
        Assert.Equal(address.Base58Check, addressString);
    }

    [Fact]
    public void Equals_SameAddress_ReturnsTrue()
    {
        var address1 = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        var address2 = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        Assert.True(address1.Equals(address2));
    }

    [Fact]
    public void Equals_DifferentAddress_ReturnsFalse()
    {
        var address1 = TronAddress.FromPrivateKey("25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32");
        var address2 = TronAddress.FromPrivateKey("0000000000000000000000000000000000000000000000000000000000000002");
        Assert.False(address1.Equals(address2));
    }

    [Fact]
    public void RoundTrip_PrivateKeyToAddressAndBack()
    {
        var originalPrivateKey = "25E9F2EDAAF9464E9FA0EFDB896835741EBE0F5E34F97CFB88457818B6681C32";
        var address = TronAddress.FromPrivateKey(originalPrivateKey);
        var addressFromBase58 = TronAddress.FromBase58(address.Base58Check);
        Assert.Equal(address.Base58Check, addressFromBase58.Base58Check);
    }
}
