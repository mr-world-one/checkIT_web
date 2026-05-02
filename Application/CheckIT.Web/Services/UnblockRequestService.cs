using CheckIT.Web.Data;
using CheckIT.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CheckIT.Web.Services;

public class UnblockRequestService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UnblockRequestService(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Task<bool> HasOpenRequestAsync(string userId, CancellationToken ct = default)
        => _db.UnblockRequests.AnyAsync(x => x.UserId == userId && x.Status == UnblockRequestStatus.Open, ct);

    public async Task<UnblockRequest> CreateAsync(string userId, string message, CancellationToken ct = default)
    {
        var req = new UnblockRequest
        {
            UserId = userId,
            Message = (message ?? string.Empty).Trim(),
            Status = UnblockRequestStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _db.UnblockRequests.Add(req);
        await _db.SaveChangesAsync(ct);
        return req;
    }

    public Task<List<UnblockRequest>> GetAllAsync(CancellationToken ct = default)
        => _db.UnblockRequests
            .AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<UnblockRequest?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.UnblockRequests
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task ResolveAsync(int id, bool approved, string? adminResponse, CancellationToken ct = default)
    {
        var req = await _db.UnblockRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (req == null) return;

        req.AdminResponse = string.IsNullOrWhiteSpace(adminResponse) ? null : adminResponse.Trim();
        req.Status = approved ? UnblockRequestStatus.Approved : UnblockRequestStatus.Rejected;
        req.ResolvedAtUtc = DateTimeOffset.UtcNow;

        if (approved && req.User != null)
        {
            req.User.IsBlocked = false;

            // Also clear Identity lockout, otherwise user may still be locked after unblock.
            await _userManager.SetLockoutEndDateAsync(req.User, null);
            await _userManager.ResetAccessFailedCountAsync(req.User);
        }

        await _db.SaveChangesAsync(ct);
    }
}
