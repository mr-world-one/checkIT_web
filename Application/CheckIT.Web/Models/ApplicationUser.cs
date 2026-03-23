using Microsoft.AspNetCore.Identity;

namespace CheckIT.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>
    /// Admin can block user access without deleting the account.
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
