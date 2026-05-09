using System.ComponentModel.DataAnnotations;

namespace CheckIT.Web.ViewModels;

public class UnblockRequestCreateVm
{
    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string? Message { get; set; }
}
