using Org.BouncyCastle.Crypto.Digests;
using System.Numerics;
using System.Text;

namespace Tron4Biz.Crypto;

/// <summary>
/// ABI 编码器，用于编码智能合约函数调用
/// </summary>
public static class AbiEncoder
{
    private const string TransferSignature = "transfer(address,uint256)";
    private const string BalanceOfSignature = "balanceOf(address)";
    private const string ApproveSignature = "approve(address,uint256)";
    private const string TransferFromSignature = "transferFrom(address,address,uint256)";
    private const string AllowanceSignature = "allowance(address,address)";
    private const string NameSignature = "name()";
    private const string SymbolSignature = "symbol()";
    private const string DecimalsSignature = "decimals()";
    private const string TotalSupplySignature = "totalSupply()";
    private const string IsBlackListedSignature = "isBlackListed(address)";

    public static readonly byte[] TransferSelector;
    public static readonly byte[] BalanceOfSelector;
    public static readonly byte[] ApproveSelector;
    public static readonly byte[] TransferFromSelector;
    public static readonly byte[] AllowanceSelector;
    public static readonly byte[] NameSelector;
    public static readonly byte[] SymbolSelector;
    public static readonly byte[] DecimalsSelector;
    public static readonly byte[] TotalSupplySelector;
    public static readonly byte[] IsBlackListedSelector;

    static AbiEncoder()
    {
        TransferSelector = ComputeSelector(TransferSignature);
        BalanceOfSelector = ComputeSelector(BalanceOfSignature);
        ApproveSelector = ComputeSelector(ApproveSignature);
        TransferFromSelector = ComputeSelector(TransferFromSignature);
        AllowanceSelector = ComputeSelector(AllowanceSignature);
        NameSelector = ComputeSelector(NameSignature);
        SymbolSelector = ComputeSelector(SymbolSignature);
        DecimalsSelector = ComputeSelector(DecimalsSignature);
        TotalSupplySelector = ComputeSelector(TotalSupplySignature);
        IsBlackListedSelector = ComputeSelector(IsBlackListedSignature);
    }

    // ========== TRC20 标准函数编码 ==========

    /// <summary>
    /// 编码 transfer(address,uint256) 函数调用
    /// </summary>
    public static byte[] EncodeTransfer(string recipientAddress, long amount)
    {
        var data = new List<byte>(68);
        data.AddRange(TransferSelector);
        data.AddRange(EncodeAddress(recipientAddress));
        data.AddRange(EncodeUint256(amount));
        return data.ToArray();
    }

    /// <summary>
    /// 编码 balanceOf(address) 函数调用
    /// </summary>
    public static byte[] EncodeBalanceOf(string ownerAddress)
    {
        var data = new List<byte>(36);
        data.AddRange(BalanceOfSelector);
        data.AddRange(EncodeAddress(ownerAddress));
        return data.ToArray();
    }

    /// <summary>
    /// 编码 approve(address,uint256) 函数调用
    /// </summary>
    public static byte[] EncodeApprove(string spenderAddress, long amount)
    {
        var data = new List<byte>(68);
        data.AddRange(ApproveSelector);
        data.AddRange(EncodeAddress(spenderAddress));
        data.AddRange(EncodeUint256(amount));
        return data.ToArray();
    }

    /// <summary>
    /// 编码 transferFrom(address,address,uint256) 函数调用
    /// </summary>
    /// <param name="senderAddress">被扣款的地址（授权方）</param>
    /// <param name="recipientAddress">收款地址</param>
    /// <param name="amount">转账金额</param>
    public static byte[] EncodeTransferFrom(string senderAddress, string recipientAddress, long amount)
    {
        var data = new List<byte>(100);
        data.AddRange(TransferFromSelector);
        data.AddRange(EncodeAddress(senderAddress));
        data.AddRange(EncodeAddress(recipientAddress));
        data.AddRange(EncodeUint256(amount));
        return data.ToArray();
    }

    /// <summary>
    /// 编码 allowance(address,address) 函数调用
    /// </summary>
    public static byte[] EncodeAllowance(string ownerAddress, string spenderAddress)
    {
        var data = new List<byte>(68);
        data.AddRange(AllowanceSelector);
        data.AddRange(EncodeAddress(ownerAddress));
        data.AddRange(EncodeAddress(spenderAddress));
        return data.ToArray();
    }

    /// <summary>
    /// 编码 name() 函数调用
    /// </summary>
    public static byte[] EncodeName()
    {
        return NameSelector.ToArray();
    }

    /// <summary>
    /// 编码 symbol() 函数调用
    /// </summary>
    public static byte[] EncodeSymbol()
    {
        return SymbolSelector.ToArray();
    }

    /// <summary>
    /// 编码 decimals() 函数调用
    /// </summary>
    public static byte[] EncodeDecimals()
    {
        return DecimalsSelector.ToArray();
    }

    /// <summary>
    /// 编码 totalSupply() 函数调用
    /// </summary>
    public static byte[] EncodeTotalSupply()
    {
        return TotalSupplySelector.ToArray();
    }

    /// <summary>
    /// 编码 isBlackListed(address) 函数调用（USDT 特有）
    /// </summary>
    public static byte[] EncodeIsBlackListed(string address)
    {
        var data = new List<byte>(36);
        data.AddRange(IsBlackListedSelector);
        data.AddRange(EncodeAddress(address));
        return data.ToArray();
    }

    // ========== 通用编码方法 ==========

    /// <summary>
    /// 编码 address 类型（32字节，左补12个0）
    /// </summary>
    public static byte[] EncodeAddress(string base58Address)
    {
        var addressBytes = TronAddress.FromBase58(base58Address).AddressBytes;
        var result = new byte[32];
        // TRON地址是21字节(0x41前缀 + 20字节)，取后20字节，放在32字节的后20位
        Array.Copy(addressBytes, 1, result, 12, 20);
        return result;
    }

    /// <summary>
    /// 编码 uint256 类型（32字节，大端序）
    /// </summary>
    public static byte[] EncodeUint256(long value)
    {
        var result = new byte[32];
        var bytes = BitConverter.GetBytes((ulong)value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        // 放在32字节的后8位（右对齐）
        Array.Copy(bytes, 0, result, 24, 8);
        return result;
    }

    /// <summary>
    /// 编码 uint256 类型（支持大数）
    /// </summary>
    public static byte[] EncodeUint256(BigInteger value)
    {
        var result = new byte[32];
        var bytes = value.ToByteArray();
        // BigInteger.ToByteArray() 返回小端序，需要转大端序
        if (bytes.Length > 32)
            throw new ArgumentException("Value too large for uint256", nameof(value));
        // 复制到结果数组的末尾（右对齐）
        Array.Copy(bytes, 0, result, 32 - bytes.Length, bytes.Length);
        // 如果是小端序，需要反转
        if (BitConverter.IsLittleEndian)
            Array.Reverse(result);
        return result;
    }

    /// <summary>
    /// 计算函数选择器（keccak256哈希的前4字节）
    /// </summary>
    public static byte[] ComputeSelector(string functionSignature)
    {
        var input = Encoding.UTF8.GetBytes(functionSignature);
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(input, 0, input.Length);
        var hash = new byte[32];
        digest.DoFinal(hash, 0);
        return hash.Take(4).ToArray();
    }

    // ========== ABI 解码方法 ==========

    /// <summary>
    /// 解码 uint256 返回值（32字节大端序）
    /// </summary>
    public static long DecodeUint256(byte[] data)
    {
        if (data == null || data.Length == 0)
            return 0;

        // 取最后8字节转换为long（uint256的低64位）
        var valueBytes = new byte[8];
        var startIndex = Math.Max(0, data.Length - 8);
        var length = Math.Min(8, data.Length);
        Array.Copy(data, startIndex, valueBytes, 8 - length, length);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(valueBytes);

        return BitConverter.ToInt64(valueBytes, 0);
    }

    /// <summary>
    /// 解码 uint256 返回值（支持大数）
    /// </summary>
    public static BigInteger DecodeUint256BigInteger(byte[] data)
    {
        if (data == null || data.Length == 0)
            return BigInteger.Zero;

        // 复制数据并反转（大端序转小端序）
        var bytes = new byte[data.Length];
        Array.Copy(data, bytes, data.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return new BigInteger(bytes);
    }
}
