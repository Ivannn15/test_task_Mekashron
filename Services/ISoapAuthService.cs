using test_task_Mekashron.Models;
using test_task_Mekashron.Models.Payloads;

namespace test_task_Mekashron.Services;

public interface ISoapAuthService
{
    Task<LoginResult> LoginAsync(string username, string password, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<RegisterResult> RegisterNewCustomerAsync(RegisterPayload payload, CancellationToken cancellationToken = default);
}
