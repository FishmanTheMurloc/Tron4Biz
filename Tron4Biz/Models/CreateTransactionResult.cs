using Protocol;

namespace Tron4Biz.Models;

public class CreateTransactionResult
{
    public Transaction Transaction { get; set; } = new Transaction();
    public string TxId { get; set; } = string.Empty;
}
