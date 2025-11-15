namespace test_task_Mekashron.Models.Payloads;

public sealed class RegisterPayload
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public int CountryId { get; init; } = 1;
    public int AffiliateId { get; init; } = 0;
    public string SignupIp { get; init; } = "127.0.0.1";
}
