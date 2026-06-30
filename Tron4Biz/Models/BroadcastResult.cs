using Protocol;

namespace Tron4Biz.Models;

public class BroadcastResult
{
    public bool Success { get; set; }
    public BroadcastStatusCode Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum BroadcastStatusCode
{
    Success = Return.Types.response_code.Success,
    SigError = Return.Types.response_code.Sigerror,
    ContractValidateError = Return.Types.response_code.ContractValidateError,
    ContractExeError = Return.Types.response_code.ContractExeError,
    BandwithError = Return.Types.response_code.BandwithError,
    DupTransactionError = Return.Types.response_code.DupTransactionError,
    TaposError = Return.Types.response_code.TaposError,
    TooBigTransactionError = Return.Types.response_code.TooBigTransactionError,
    TransactionExpirationError = Return.Types.response_code.TransactionExpirationError,
    ServerBusy = Return.Types.response_code.ServerBusy,
    NoConnection = Return.Types.response_code.NoConnection,
    NotEnoughEffectiveConnection = Return.Types.response_code.NotEnoughEffectiveConnection,
    OtherError = Return.Types.response_code.OtherError,
    NetworkTimeout = 50,
    NetworkError = 51
}
