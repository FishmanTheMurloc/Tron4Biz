using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class AbiEncoderTests
{
    #region Selector Tests

    [Fact]
    public void TransferSelector_ShouldBeCorrect()
    {
        // transfer(address,uint256) -> a9059cbb
        var expected = new byte[] { 0xa9, 0x05, 0x9c, 0xbb };
        Assert.Equal(expected, AbiEncoder.TransferSelector);
    }

    [Fact]
    public void BalanceOfSelector_ShouldBeCorrect()
    {
        // balanceOf(address) -> 70a08231
        var expected = new byte[] { 0x70, 0xa0, 0x82, 0x31 };
        Assert.Equal(expected, AbiEncoder.BalanceOfSelector);
    }

    [Fact]
    public void ApproveSelector_ShouldBeCorrect()
    {
        // approve(address,uint256) -> 095ea7b3
        var expected = new byte[] { 0x09, 0x5e, 0xa7, 0xb3 };
        Assert.Equal(expected, AbiEncoder.ApproveSelector);
    }

    [Fact]
    public void TransferFromSelector_ShouldBeCorrect()
    {
        // transferFrom(address,address,uint256) -> 23b872dd
        var expected = new byte[] { 0x23, 0xb8, 0x72, 0xdd };
        Assert.Equal(expected, AbiEncoder.TransferFromSelector);
    }

    [Fact]
    public void AllowanceSelector_ShouldBeCorrect()
    {
        // allowance(address,address) -> dd62ed3e
        var expected = new byte[] { 0xdd, 0x62, 0xed, 0x3e };
        Assert.Equal(expected, AbiEncoder.AllowanceSelector);
    }

    [Fact]
    public void NameSelector_ShouldBeCorrect()
    {
        // name() -> 06fdde03
        var expected = new byte[] { 0x06, 0xfd, 0xde, 0x03 };
        Assert.Equal(expected, AbiEncoder.NameSelector);
    }

    [Fact]
    public void SymbolSelector_ShouldBeCorrect()
    {
        // symbol() -> 95d89b41
        var expected = new byte[] { 0x95, 0xd8, 0x9b, 0x41 };
        Assert.Equal(expected, AbiEncoder.SymbolSelector);
    }

    [Fact]
    public void DecimalsSelector_ShouldBeCorrect()
    {
        // decimals() -> 313ce567
        var expected = new byte[] { 0x31, 0x3c, 0xe5, 0x67 };
        Assert.Equal(expected, AbiEncoder.DecimalsSelector);
    }

    [Fact]
    public void TotalSupplySelector_ShouldBeCorrect()
    {
        // totalSupply() -> 18160ddd
        var expected = new byte[] { 0x18, 0x16, 0x0d, 0xdd };
        Assert.Equal(expected, AbiEncoder.TotalSupplySelector);
    }

    #endregion

    #region ComputeSelector Tests

    [Fact]
    public void ComputeSelector_KnownFunctions_ReturnsExpected()
    {
        // 验证计算结果与预期一致
        var transfer = AbiEncoder.ComputeSelector("transfer(address,uint256)");
        Assert.Equal(AbiEncoder.TransferSelector, transfer);

        var balanceOf = AbiEncoder.ComputeSelector("balanceOf(address)");
        Assert.Equal(AbiEncoder.BalanceOfSelector, balanceOf);
    }

    #endregion

    #region EncodeAddress Tests

    [Fact]
    public void EncodeAddress_ValidAddress_Returns32Bytes()
    {
        var address = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9";
        var encoded = AbiEncoder.EncodeAddress(address);

        Assert.Equal(32, encoded.Length);
        // 前12字节应该是0
        for (int i = 0; i < 12; i++)
        {
            Assert.Equal(0, encoded[i]);
        }
        // 后20字节是地址（不含0x41前缀）
        var tronAddress = TronAddress.FromBase58(address);
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(tronAddress.AddressBytes[i + 1], encoded[i + 12]);
        }
    }

    #endregion

    #region EncodeUint256 Tests

    [Fact]
    public void EncodeUint256_Zero_Returns32ZeroBytes()
    {
        var encoded = AbiEncoder.EncodeUint256(0L);
        Assert.Equal(32, encoded.Length);
        Assert.All(encoded, b => Assert.Equal(0, b));
    }

    [Fact]
    public void EncodeUint256_SmallValue_ReturnsRightAligned()
    {
        var encoded = AbiEncoder.EncodeUint256(1000L);
        Assert.Equal(32, encoded.Length);
        // 1000 = 0x03E8，大端序存储在32字节的最后2个字节
        // 前30字节是0
        for (int i = 0; i < 30; i++)
        {
            Assert.Equal(0, encoded[i]);
        }
        // 最后2字节是1000的大端序表示 0x03E8
        Assert.Equal(0x00, encoded[29]);
        Assert.Equal(0x03, encoded[30]);
        Assert.Equal(0xe8, encoded[31]);
    }

    [Fact]
    public void EncodeUint256_LargeValue_ReturnsCorrectEncoding()
    {
        // USDT 金额：100 USDT = 100 * 10^6 = 100000000
        var encoded = AbiEncoder.EncodeUint256(100_000_000L);
        Assert.Equal(32, encoded.Length);
        // 100000000 = 0x05F5E100
        Assert.Equal(0x05, encoded[28]);
        Assert.Equal(0xf5, encoded[29]);
        Assert.Equal(0xe1, encoded[30]);
        Assert.Equal(0x00, encoded[31]);
    }

    #endregion

    #region EncodeTransfer Tests

    [Fact]
    public void EncodeTransfer_ReturnsCorrectLength()
    {
        var toAddress = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9";
        var amount = 100_000_000L; // 100 USDT

        var encoded = AbiEncoder.EncodeTransfer(toAddress, amount);

        // 4字节选择器 + 32字节address + 32字节uint256 = 68字节
        Assert.Equal(68, encoded.Length);
        // 前4字节是选择器
        Assert.Equal(AbiEncoder.TransferSelector, encoded.Take(4).ToArray());
    }

    #endregion

    #region EncodeBalanceOf Tests

    [Fact]
    public void EncodeBalanceOf_ReturnsCorrectLength()
    {
        var ownerAddress = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9";

        var encoded = AbiEncoder.EncodeBalanceOf(ownerAddress);

        // 4字节选择器 + 32字节address = 36字节
        Assert.Equal(36, encoded.Length);
        // 前4字节是选择器
        Assert.Equal(AbiEncoder.BalanceOfSelector, encoded.Take(4).ToArray());
    }

    #endregion

    #region EncodeTransferFrom Tests

    [Fact]
    public void EncodeTransferFrom_ReturnsCorrectLength()
    {
        // 使用测试向量中的有效地址
        var fromAddress = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9";
        var toAddress = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9"; // 使用相同地址作为测试
        var amount = 100_000_000L;

        var encoded = AbiEncoder.EncodeTransferFrom(fromAddress, toAddress, amount);

        // 4字节选择器 + 32字节from + 32字节to + 32字节amount = 100字节
        Assert.Equal(100, encoded.Length);
        // 前4字节是选择器
        Assert.Equal(AbiEncoder.TransferFromSelector, encoded.Take(4).ToArray());
    }

    #endregion

    #region EncodeAllowance Tests

    [Fact]
    public void EncodeAllowance_ReturnsCorrectLength()
    {
        var owner = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9";
        var spender = "TMuA6YqfCeX8EhbfYEg5y7S4DqzSJireY9";

        var encoded = AbiEncoder.EncodeAllowance(owner, spender);

        // 4字节选择器 + 32字节owner + 32字节spender = 68字节
        Assert.Equal(68, encoded.Length);
        Assert.Equal(AbiEncoder.AllowanceSelector, encoded.Take(4).ToArray());
    }

    #endregion

    #region EncodeNoParamFunctions Tests

    [Fact]
    public void EncodeName_ReturnsSelectorOnly()
    {
        var encoded = AbiEncoder.EncodeName();
        Assert.Equal(4, encoded.Length);
        Assert.Equal(AbiEncoder.NameSelector, encoded);
    }

    [Fact]
    public void EncodeSymbol_ReturnsSelectorOnly()
    {
        var encoded = AbiEncoder.EncodeSymbol();
        Assert.Equal(4, encoded.Length);
        Assert.Equal(AbiEncoder.SymbolSelector, encoded);
    }

    [Fact]
    public void EncodeDecimals_ReturnsSelectorOnly()
    {
        var encoded = AbiEncoder.EncodeDecimals();
        Assert.Equal(4, encoded.Length);
        Assert.Equal(AbiEncoder.DecimalsSelector, encoded);
    }

    [Fact]
    public void EncodeTotalSupply_ReturnsSelectorOnly()
    {
        var encoded = AbiEncoder.EncodeTotalSupply();
        Assert.Equal(4, encoded.Length);
        Assert.Equal(AbiEncoder.TotalSupplySelector, encoded);
    }

    #endregion

    #region DecodeUint256 Tests

    [Fact]
    public void DecodeUint256_Zero_ReturnsZero()
    {
        var data = new byte[32];
        var result = AbiEncoder.DecodeUint256(data);
        Assert.Equal(0L, result);
    }

    [Fact]
    public void DecodeUint256_SmallValue_ReturnsCorrectValue()
    {
        // 1000 = 0x03E8，大端序存储在32字节的最后2个字节
        var data = new byte[32];
        data[30] = 0x03;
        data[31] = 0xe8;

        var result = AbiEncoder.DecodeUint256(data);
        Assert.Equal(1000L, result);
    }

    [Fact]
    public void DecodeUint256_LargeValue_ReturnsCorrectValue()
    {
        // 100 USDT = 100000000 = 0x05F5E100
        var data = new byte[32];
        data[28] = 0x05;
        data[29] = 0xf5;
        data[30] = 0xe1;
        data[31] = 0x00;

        var result = AbiEncoder.DecodeUint256(data);
        Assert.Equal(100_000_000L, result);
    }

    [Fact]
    public void DecodeUint256_NullOrEmpty_ReturnsZero()
    {
        Assert.Equal(0L, AbiEncoder.DecodeUint256(null));
        Assert.Equal(0L, AbiEncoder.DecodeUint256(Array.Empty<byte>()));
    }

    [Fact]
    public void DecodeUint256_EncodeDecode_RoundTrip()
    {
        var originalValue = 123456789L;
        var encoded = AbiEncoder.EncodeUint256(originalValue);
        var decoded = AbiEncoder.DecodeUint256(encoded);
        Assert.Equal(originalValue, decoded);
    }

    #endregion
}
