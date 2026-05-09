using System.ComponentModel.DataAnnotations;

namespace CheckIT.Web.Models;

public class UnblockRequest
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = default!;

    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = default!;

    [MaxLength(1000)]
    public string? AdminResponse { get; set; }

    public UnblockRequestStatus Status { get; set; } = UnblockRequestStatus.Open;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ResolvedAtUtc { get; set; }
}

public enum UnblockRequestStatus
{
    Open = 0,
    Approved = 1,
    Rejected = 2
}
