using System.ComponentModel.DataAnnotations;

namespace CheckIT.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Вкажіть пароль")]
    public string? Password { get; set; }
}
