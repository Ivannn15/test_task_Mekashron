using System.ComponentModel.DataAnnotations;

namespace test_task_Mekashron.Contracts;

public sealed class RegisterRequest
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Mobile { get; set; }

    [Range(1, int.MaxValue)]
    public int CountryId { get; set; } = 1;

    [Range(0, int.MaxValue)]
    public int AffiliateId { get; set; }

    public string? SignupIp { get; set; }
}
