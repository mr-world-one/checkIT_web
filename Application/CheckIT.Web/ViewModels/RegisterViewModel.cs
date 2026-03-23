using System.ComponentModel.DataAnnotations;

namespace CheckIT.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Вкажіть ім'я")]
    [MaxLength(100, ErrorMessage = "Максимум 100 символів")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Вкажіть пароль")]
    [MinLength(8, ErrorMessage = "Пароль має бути не менше 8 символів")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Підтвердіть пароль")]
    [Compare(nameof(Password), ErrorMessage = "Паролі не співпадають")]
    public string? ConfirmPassword { get; set; }
}
