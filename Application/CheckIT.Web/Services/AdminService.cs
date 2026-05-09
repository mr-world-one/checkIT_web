using CheckIT.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CheckIT.Web.Services;

public class AdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public virtual Task<List<ApplicationUser>> GetAllUsersAsync()
        => _userManager.Users.OrderBy(u => u.CreatedAtUtc).ToListAsync();

    public virtual async Task SetBlockedAsync(string userId, bool blocked)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено");
        user.IsBlocked = blocked;
        await _userManager.UpdateAsync(user);

        // Also lockout in Identity so SignInManager prevents password sign-in.
        if (blocked)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
    }

    public virtual async Task DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;
        await _userManager.DeleteAsync(user);
    }
}
