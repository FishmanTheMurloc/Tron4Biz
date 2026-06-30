using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Protocol;

namespace Tron4Biz.Proto;

public class WalletSolidityGrpcClient : ITronSolidityClient
{
    private readonly GrpcChannel _channel;
    private readonly WalletSolidity.WalletSolidityClient _client;
    private readonly string? _apiKey;
    private readonly TimeSpan _defaultTimeout;

    public string? ApiKey => _apiKey;

    public WalletSolidityGrpcClient(string target, string? apiKey = null, TimeSpan? timeout = null)
    {
        _defaultTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var channel = GrpcChannel.ForAddress(target);
        _channel = channel;
        _client = new WalletSolidity.WalletSolidityClient(_channel);
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