using Tron4Biz.Options;

namespace Tron4Biz.DependencyInjection;

public class Tron4BizBuilder
{
    public Tron4BizOptions Options { get; } = new();

    public Tron4BizBuilder UseMainnet()
    {
        Options.Network = TronNetwork.Mainnet;
        Options.FullNode.Endpoint = "http://grpc.trongrid.io:50051";
        Options.SolidityNode.Endpoint = "http://grpc.trongrid.io:50052";
        Options.UsdtContractAddress = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
        return this;
    }

    public Tron4BizBuilder UseShasta()
    {
        Options.Network = TronNetwork.Shasta;
        Options.FullNode.Endpoint = "http://grpc.shasta.trongrid.io:50051";
        Options.SolidityNode.Endpoint = "http://grpc.shasta.trongrid.io:50052";
        Options.UsdtContractAddress = "TG3XXyExBkPp9nzdajDZsozEu4BkaSJozs";
        return this;
    }

    public Tron4BizBuilder UseNile()
    {
        Options.Network = TronNetwork.Nile;
        Options.FullNode.Endpoint = "http://grpc.nile.trongrid.io:50051";
        Options.SolidityNode.Endpoint = "http://grpc.nile.trongrid.io:50061";
        Options.UsdtContractAddress = "TXYZopYRdj2D9XRtbG411XZZ3kM5VkAeBf";
        return this;
    }

    public Tron4BizBuilder ConfigureFullNode(Action<GrpcEndpointOptions> configure)
    {
        configure(Options.FullNode);
        return this;
    }

    public Tron4BizBuilder ConfigureSolidityNode(Action<GrpcEndpointOptions> configure)
    {
        configure(Options.SolidityNode);
        return this;
    }
}
