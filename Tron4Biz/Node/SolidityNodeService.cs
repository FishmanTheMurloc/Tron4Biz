using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Protocol;
using Tron4Biz.Options;

namespace Tron4Biz.Node;

public class SolidityNodeService : ISolidityNodeService
{
    private readonly GrpcChannel _channel;
    private readonly WalletSolidity.WalletSolidityClient _client;
    private readonly TimeSpan _defaultTimeout;
    private readonly string? _apiKey;

    public SolidityNodeService(GrpcEndpointOptions options)
    {
        _defaultTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        _channel = GrpcChannel.ForAddress(options.Endpoint);
        _client = new WalletSolidity.WalletSolidityClient(_channel);
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

    public Account GetAccount(byte[] address)
    {
        var request = new Account { Address = ByteString.CopyFrom(address) };
        return _client.GetAccount(request, CreateCallOptions());
    }

    public BlockExtention GetNowBlock2()
    {
        return _client.GetNowBlock2(new EmptyMessage(), CreateCallOptions());
    }

    public Transaction GetTransactionById(byte[] txId)
    {
        var request = new BytesMessage { Value = ByteString.CopyFrom(txId) };
        return _client.GetTransactionById(request, CreateCallOptions());
    }

    public TransactionInfo GetTransactionInfoById(byte[] txId)
    {
        var request = new BytesMessage { Value = ByteString.CopyFrom(txId) };
        return _client.GetTransactionInfoById(request, CreateCallOptions());
    }

    public TransactionExtention TriggerConstantContract(TriggerSmartContract contract)
    {
        return _client.TriggerConstantContract(contract, CreateCallOptions());
    }
}
