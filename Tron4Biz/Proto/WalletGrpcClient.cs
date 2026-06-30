using Grpc.Core;
using Grpc.Net.Client;
using Protocol;

namespace Tron4Biz.Proto;

public class WalletGrpcClient : ITronClient
{
    private readonly GrpcChannel _channel;
    private readonly Wallet.WalletClient _client;
    private readonly string? _apiKey;
    private readonly TimeSpan _defaultTimeout;

    public string? ApiKey => _apiKey;

    public WalletGrpcClient(string target, string? apiKey = null, TimeSpan? timeout = null)
    {
        _defaultTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var channel = GrpcChannel.ForAddress(target);
        _channel = channel;
        _client = new Wallet.WalletClient(_channel);
        _apiKey = apiKey;
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