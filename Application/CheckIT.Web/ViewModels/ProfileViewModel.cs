using System.ComponentModel.DataAnnotations;

namespace CheckIT.Web.ViewModels;

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Вкажіть ім'я")]
    [StringLength(100, ErrorMessage = "Ім'я занадто довге")]
    [Display(Name = "Ім'я")]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }
}
