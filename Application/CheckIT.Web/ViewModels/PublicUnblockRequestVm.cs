using System.ComponentModel.DataAnnotations;

namespace CheckIT.Web.ViewModels;

public class PublicUnblockRequestVm
{
    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Некоректний формат email")]
    public string? Email { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string? Message { get; set; }
}
