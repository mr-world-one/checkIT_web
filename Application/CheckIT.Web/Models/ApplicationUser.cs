using Microsoft.AspNetCore.Identity;

namespace CheckIT.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsBlocked { get; set; } = false;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
