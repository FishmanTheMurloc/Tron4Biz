using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class Base58Tests
{
    [Fact]
    public void Encode_Empty_ReturnsEmpty()
    {
        var result = Base58.Encode(Array.Empty<byte>());
        Assert.Equal("", result);
    }

    [Fact]
    public void Encode_SingleZeroByte_ReturnsOne()
    {
        var result = Base58.Encode(new byte[] {0});
        Assert.Equal("1", result);
    }

    [Fact]
    public void Decode_EmptyString_ReturnsEmptyArray()
    {
        var result = Base58.Decode("");
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => Base58.Decode("abc0IOl"));
    }

}
