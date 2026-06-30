using Grpc.Core;
using Grpc.Net.Client;
using Protocol;
using Tron4Biz.Options;

namespace Tron4Biz.Node;

public class FullNodeService : IFullNodeService
{
    private readonly GrpcChannel _channel;
    private readonly Wallet.WalletClient _client;
    private readonly TimeSpan _defaultTimeout;
    private readonly string? _apiKey;

    public FullNodeService(GrpcEndpointOptions options)
    {
        _defaultTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        _channel = GrpcChannel.ForAddress(options.Endpoint);
        _client = new Wallet.WalletClient(_channel);
        _apiKey = options.ApiKey;
    }

    public void Dispose()
    {
        _channel.Dispose();
    }

    private CallOptions CreateCallOptions()
    {
        var deadline = DateTime.UtcNow.Add(_defaultTimeout);
        var callOptions = new CallOptions(deadline: deadline);

        if (!string.IsNullOrEmpty(_apiKey))
        {
            var headers = new Metadata
            {
                { "TRON-PRO-API-KEY", _apiKey }
            };
            callOptions = callOptions.WithHeaders(headers);
        }

        return callOptions;
    }

    public TransactionExtention CreateTransaction2(TransferContract contract)
    {
        return _client.CreateTransaction2(contract, CreateCallOptions());
    }

    public Return BroadcastTransaction(Transaction transaction)
    {
        return _client.BroadcastTransaction(transaction, CreateCallOptions());
    }

    public TransactionExtention TriggerContract(TriggerSmartContract contract)
    {
        return _client.TriggerContract(contract, CreateCallOptions());
    }

    public TransactionExtention TriggerConstantContract(TriggerSmartContract contract)
    {
        return _client.TriggerConstantContract(contract, CreateCallOptions());
    }
}
