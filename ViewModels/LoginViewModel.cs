using System.ComponentModel.DataAnnotations;
using test_task_Mekashron.Models;

namespace test_task_Mekashron.ViewModels;

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    public LoginResult? Result { get; set; }
}
