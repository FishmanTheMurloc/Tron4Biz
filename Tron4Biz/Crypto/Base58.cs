using System.Numerics;

namespace Tron4Biz.Crypto;

public static class Base58
{
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static string Encode(byte[] input)
    {
        if (input.Length == 0)
            return string.Empty;

        BigInteger bn = BigInteger.Zero;
        for (int i = 0; i < input.Length; i++)
        {
            bn = bn * 256 + input[i];
        }

        var chars = new List<char>();

        if (bn > 0)
        {
            while (bn >= 58)
            {
                int remainder = (int)(bn % 58);
                bn = bn / 58;
                chars.Add(Alphabet[remainder]);
            }
            chars.Add(Alphabet[(int)bn]);
        }

        int leadingZeros = 0;
        for (int i = 0; i < input.Length && input[i] == 0; i++)
        {
            leadingZeros++;
        }

        for (int i = 0; i < leadingZeros; i++)
        {
            chars.Add(Alphabet[0]);
        }

        chars.Reverse();
        return new string(chars.ToArray());
    }

    public static byte[] Decode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<byte>();

        BigInteger bn = BigInteger.Zero;
        for (int i = 0; i < input.Length; i++)
        {
            int index = Alphabet.IndexOf(input[i]);
            if (index < 0)
                throw new ArgumentException($"Illegal character '{input[i]}' at position {i}");
            bn = bn * 58 + index;
        }

        var result = new List<byte>();
        while (bn > 0)
        {
            result.Insert(0, (byte)(bn % 256));
            bn = bn / 256;
        }

        int leadingZeros = 0;
        for (int i = 0; i < input.Length && input[i] == Alphabet[0]; i++)
        {
            leadingZeros++;
        }

        while (result.Count < leadingZeros)
        {
            result.Insert(0, 0);
        }

        return result.ToArray();
    }
}
