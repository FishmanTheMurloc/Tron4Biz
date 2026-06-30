namespace Tron4Biz.Models;

public class BlockInfo
{
    public long BlockNumber { get; set; }
    public long BlockTimestamp { get; set; }
    public byte[] BlockHash { get; set; } = Array.Empty<byte>();
    public byte[] RefBlockBytes { get; set; } = Array.Empty<byte>();
    public byte[] RefBlockHash { get; set; } = Array.Empty<byte>();
}
