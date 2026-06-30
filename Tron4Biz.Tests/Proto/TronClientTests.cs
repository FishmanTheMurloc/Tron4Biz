using Moq;
using Protocol;
using Tron4Biz.Proto;
using Xunit;

namespace Tron4Biz.Tests.Proto;

public class TronClientTests
{
    private readonly Mock<ITronClient> _mockClient;

    public TronClientTests()
    {
        _mockClient = new Mock<ITronClient>();
    }

    [Fact]
    public void BroadcastTransaction_WithSignedTransaction_ReturnsSuccess()
    {
        var transaction = new Transaction();

        var expectedReturn = new Return
        {
            Result = true,
            Code = Return.Types.response_code.Success
        };

        _mockClient.Setup(c => c.BroadcastTransaction(transaction)).Returns(expectedReturn);

        var result = _mockClient.Object.BroadcastTransaction(transaction);

        Assert.True(result.Result);
        Assert.Equal(Return.Types.response_code.Success, result.Code);
        _mockClient.Verify(c => c.BroadcastTransaction(transaction), Times.Once);
    }

    [Fact]
    public void BroadcastTransaction_WithInvalidTransaction_ReturnsFailure()
    {
        var transaction = new Transaction();

        var expectedReturn = new Return
        {
            Result = false,
            Code = Return.Types.response_code.Sigerror
        };

        _mockClient.Setup(c => c.BroadcastTransaction(transaction)).Returns(expectedReturn);

        var result = _mockClient.Object.BroadcastTransaction(transaction);

        Assert.False(result.Result);
        Assert.Equal(Return.Types.response_code.Sigerror, result.Code);
    }

    [Fact]
    public void Dispose_CallsDisposeOnce()
    {
        _mockClient.Setup(c => c.Dispose());

        _mockClient.Object.Dispose();

        _mockClient.Verify(c => c.Dispose(), Times.Once);
    }
}