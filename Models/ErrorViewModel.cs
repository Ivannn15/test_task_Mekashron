namespace test_task_Mekashron.Models;

public sealed class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
}
