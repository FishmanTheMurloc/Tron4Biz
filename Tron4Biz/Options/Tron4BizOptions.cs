namespace Tron4Biz.Options;

public class Tron4BizOptions
{
    public TronNetwork Network { get; set; } = TronNetwork.Shasta;

    public GrpcEndpointOptions FullNode { get; set; } = new();

    public GrpcEndpointOptions SolidityNode { get; set; } = new();

    public string UsdtContractAddress { get; set; } = string.Empty;
}
