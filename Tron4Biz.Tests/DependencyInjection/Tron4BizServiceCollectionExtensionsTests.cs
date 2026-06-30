using Microsoft.Extensions.DependencyInjection;
using Tron4Biz.DependencyInjection;
using Tron4Biz.Options;
using Xunit;

namespace Tron4Biz.Tests.DependencyInjection;

public class Tron4BizServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTron4Biz_RegistersOptions()
    {
        var services = new ServiceCollection();

        services.AddTron4Biz(builder =>
        {
            builder
                .UseMainnet()
                .ConfigureFullNode(o => o.Endpoint = "http://fullnode:50051")
                .ConfigureSolidityNode(o => o.Endpoint = "http://solidity:50051");
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Tron4BizOptions>();

        Assert.NotNull(options);
        Assert.Equal(TronNetwork.Mainnet, options.Network);
    }

    [Fact]
    public void AddTron4Biz_ConfiguresOptionsCorrectly()
    {
        var services = new ServiceCollection();

        services.AddTron4Biz(builder =>
        {
            builder
                .UseShasta()
                .ConfigureFullNode(o =>
                {
                    o.Endpoint = "http://shasta-fullnode:50051";
                    o.TimeoutSeconds = 60;
                    o.ApiKey = "test-api-key";
                })
                .ConfigureSolidityNode(o => o.Endpoint = "http://shasta-solidity:50051");
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Tron4BizOptions>();

        Assert.Equal(TronNetwork.Shasta, options.Network);
        Assert.Equal("http://shasta-fullnode:50051", options.FullNode.Endpoint);
        Assert.Equal(60, options.FullNode.TimeoutSeconds);
        Assert.Equal("test-api-key", options.FullNode.ApiKey);
        Assert.Equal("http://shasta-solidity:50051", options.SolidityNode.Endpoint);
    }

    [Fact]
    public void AddTron4Biz_OptionsAreSingleton()
    {
        var services = new ServiceCollection();

        services.AddTron4Biz(builder =>
        {
            builder
                .UseMainnet()
                .ConfigureFullNode(o => o.Endpoint = "http://fullnode:50051")
                .ConfigureSolidityNode(o => o.Endpoint = "http://solidity:50051");
        });

        var provider = services.BuildServiceProvider();

        var options1 = provider.GetRequiredService<Tron4BizOptions>();
        var options2 = provider.GetRequiredService<Tron4BizOptions>();

        Assert.Same(options1, options2);
    }

    [Fact]
    public void AddTron4Biz_UsdtContractAddress_MatchNetwork()
    {
        var services = new ServiceCollection();

        services.AddTron4Biz(builder => builder.UseMainnet());

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Tron4BizOptions>();

        Assert.Equal("TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t", options.UsdtContractAddress);
    }

    [Fact]
    public void Tron4BizBuilder_UseMainnet_SetsNetworkAndDefaultEndpoints()
    {
        var builder = new Tron4BizBuilder();
        builder.UseMainnet();

        Assert.Equal(TronNetwork.Mainnet, builder.Options.Network);
        Assert.Equal("http://grpc.trongrid.io:50051", builder.Options.FullNode.Endpoint);
        Assert.Equal("http://grpc.trongrid.io:50052", builder.Options.SolidityNode.Endpoint);
        Assert.Equal("TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t", builder.Options.UsdtContractAddress);
    }

    [Fact]
    public void Tron4BizBuilder_UseShasta_SetsNetworkAndDefaultEndpoints()
    {
        var builder = new Tron4BizBuilder();
        builder.UseShasta();

        Assert.Equal(TronNetwork.Shasta, builder.Options.Network);
        Assert.Equal("http://grpc.shasta.trongrid.io:50051", builder.Options.FullNode.Endpoint);
        Assert.Equal("http://grpc.shasta.trongrid.io:50052", builder.Options.SolidityNode.Endpoint);
        Assert.Equal("TG3XXyExBkPp9nzdajDZsozEu4BkaSJozs", builder.Options.UsdtContractAddress);
    }

    [Fact]
    public void Tron4BizBuilder_UseNile_SetsNetworkAndDefaultEndpoints()
    {
        var builder = new Tron4BizBuilder();
        builder.UseNile();

        Assert.Equal(TronNetwork.Nile, builder.Options.Network);
        Assert.Equal("http://grpc.nile.trongrid.io:50051", builder.Options.FullNode.Endpoint);
        Assert.Equal("http://grpc.nile.trongrid.io:50061", builder.Options.SolidityNode.Endpoint);
        Assert.Equal("TXYZopYRdj2D9XRtbG411XZZ3kM5VkAeBf", builder.Options.UsdtContractAddress);
    }

    [Fact]
    public void Tron4BizBuilder_UseNetwork_CanBeOverriddenByConfigureNode()
    {
        var builder = new Tron4BizBuilder();
        builder
            .UseMainnet()
            .ConfigureFullNode(o => o.Endpoint = "http://custom-fullnode:50051")
            .ConfigureSolidityNode(o => o.Endpoint = "http://custom-solidity:50052");

        Assert.Equal(TronNetwork.Mainnet, builder.Options.Network);
        Assert.Equal("http://custom-fullnode:50051", builder.Options.FullNode.Endpoint);
        Assert.Equal("http://custom-solidity:50052", builder.Options.SolidityNode.Endpoint);
    }

    [Fact]
    public void Tron4BizBuilder_ConfigureFullNode_SetsAllProperties()
    {
        var builder = new Tron4BizBuilder();
        builder.ConfigureFullNode(o =>
        {
            o.Endpoint = "http://test:50051";
            o.ApiKey = "key123";
            o.TimeoutSeconds = 45;
        });

        Assert.Equal("http://test:50051", builder.Options.FullNode.Endpoint);
        Assert.Equal("key123", builder.Options.FullNode.ApiKey);
        Assert.Equal(45, builder.Options.FullNode.TimeoutSeconds);
    }

    [Fact]
    public void Tron4BizBuilder_ConfigureSolidityNode_SetsAllProperties()
    {
        var builder = new Tron4BizBuilder();
        builder.ConfigureSolidityNode(o =>
        {
            o.Endpoint = "http://solidity:50051";
            o.TimeoutSeconds = 90;
        });

        Assert.Equal("http://solidity:50051", builder.Options.SolidityNode.Endpoint);
        Assert.Equal(90, builder.Options.SolidityNode.TimeoutSeconds);
    }

    [Fact]
    public void Tron4BizBuilder_Methods_ReturnBuilderForChaining()
    {
        var builder = new Tron4BizBuilder();

        var result1 = builder.UseMainnet();
        var result2 = builder.UseNile();
        var result3 = builder.ConfigureFullNode(_ => { });
        var result4 = builder.ConfigureSolidityNode(_ => { });

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
        Assert.Same(builder, result4);
    }
}
