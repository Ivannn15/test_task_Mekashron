namespace test_task_Mekashron.Models;

public sealed class RegisterResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? ResultCode { get; init; }
    public string RawPayload { get; init; } = string.Empty;
}
