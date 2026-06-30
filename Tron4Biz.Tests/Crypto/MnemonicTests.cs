using Tron4Biz.Crypto;
using Xunit;

namespace Tron4Biz.Tests.Crypto;

public class MnemonicTests
{
    [Fact]
    public void FromSentence_ValidMnemonic_CreatesMnemonic()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var mn = Mnemonic.FromSentence(mnemonic);

        Assert.NotNull(mn);
        Assert.Equal(12, mn.Words.Length);
    }

    [Fact]
    public void ToSentence_RoundTrip()
    {
        var original = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var mn = Mnemonic.FromSentence(original);
        var result = mn.Sentence;

        Assert.Equal(original, result);
    }

    [Fact]
    public void ToSeed_ValidMnemonic_ReturnsSeed()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var mn = Mnemonic.FromSentence(mnemonic);

        var seed = mn.ToSeed("alice");

        Assert.NotNull(seed);
        Assert.Equal(64, seed.Length);
    }

    [Fact]
    public void ToSeed_SameMnemonic_SamePassphrase_SameSeed()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var mn1 = Mnemonic.FromSentence(mnemonic);
        var mn2 = Mnemonic.FromSentence(mnemonic);

        var seed1 = mn1.ToSeed("alice");
        var seed2 = mn2.ToSeed("alice");

        Assert.Equal(seed1, seed2);
    }

    [Fact]
    public void ToSeed_SameMnemonic_DifferentPassphrase_DifferentSeed()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var mn1 = Mnemonic.FromSentence(mnemonic);
        var mn2 = Mnemonic.FromSentence(mnemonic);

        var seed1 = mn1.ToSeed("alice");
        var seed2 = mn2.ToSeed("bob");

        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void ToSeed_EmptyPassphrase_Works()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var mn = Mnemonic.FromSentence(mnemonic);

        var seed = mn.ToSeed("");

        Assert.NotNull(seed);
        Assert.Equal(64, seed.Length);
    }

    [Fact]
    public void Words_Property_ReturnsCorrectWords()
    {
        var words = new[] {"abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "abandon", "about"};
        var mn = Mnemonic.FromSentence(string.Join(" ", words));

        Assert.Equal(words, mn.Words);
    }
}
