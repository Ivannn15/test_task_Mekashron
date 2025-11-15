namespace test_task_Mekashron.Models;

public sealed class LoginResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? ResultCode { get; init; }
    public IReadOnlyList<SoapUserDetail> Details { get; init; } = Array.Empty<SoapUserDetail>();
    public string RawPayload { get; init; } = string.Empty;
}
