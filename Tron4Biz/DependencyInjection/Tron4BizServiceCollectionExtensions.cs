using Microsoft.Extensions.DependencyInjection;
using Tron4Biz.Node;
using Tron4Biz.Transactions;

namespace Tron4Biz.DependencyInjection;

public static class Tron4BizServiceCollectionExtensions
{
    public static IServiceCollection AddTron4Biz(
        this IServiceCollection services,
        Action<Tron4BizBuilder> configureBuilder)
    {
        var builder = new Tron4BizBuilder();
        configureBuilder(builder);

        var options = builder.Options;

        services.AddSingleton(options);
        services.AddSingleton<IFullNodeService>(_ => new FullNodeService(options.FullNode));
        services.AddSingleton<ISolidityNodeService>(_ => new SolidityNodeService(options.SolidityNode));
        services.AddSingleton<ITronGrpcClient, TronGrpcClient>();
        services.AddSingleton<ITronTransactionService, LocalTronTransactionService>();
        services.AddSingleton<IHDWalletService, HDWalletService>();

        return services;
    }
}
