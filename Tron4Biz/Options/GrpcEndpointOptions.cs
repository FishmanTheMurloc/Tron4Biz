namespace Tron4Biz.Options;

public class GrpcEndpointOptions
{
    /// <summary>
    /// gRPC 端点地址，必须是完整 URI，例如 http://grpc.trongrid.io:50051。
    /// http:// 表示明文 HTTP/2（无 TLS），https:// 表示 TLS 加密的 HTTP/2。
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
