using CheckIT.Web.Models;
using CheckIT.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin)
    {
        _admin = admin;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var users = await _admin.GetAllUsersAsync();
        ViewBag.TotalUsers = users.Count;
        ViewBag.BlockedUsers = users.Count(u => u.IsBlocked);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _admin.GetAllUsersAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBlocked(string id, bool blocked)
    {
        await _admin.SetBlockedAsync(id, blocked);
        TempData["Success"] = blocked ? "Користувача заблоковано" : "Користувача розблоковано";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _admin.DeleteUserAsync(id);
        TempData["Success"] = "Користувача видалено";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public IActionResult Logs(DateTime? from = null, DateTime? to = null, string? level = null)
    {
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "app.log");
        if (!System.IO.File.Exists(logPath))
        {
            ViewBag.Message = "Логи відсутні";
            return View(Array.Empty<string>());
        }

        IEnumerable<string> lines;
        try
        {
            lines = System.IO.File.ReadLines(logPath);
        }
        catch
        {
            ViewBag.Message = "Помилка доступу до логів";
            return View(Array.Empty<string>());
        }

        // Format: yyyy-MM-dd HH:mm:ss.fff zzz<TAB>LEVEL<TAB>message
        if (!string.IsNullOrWhiteSpace(level))
        {
            level = level.Trim().ToUpperInvariant();
            lines = lines.Where(l => l.Contains($"\t{level}\t", StringComparison.Ordinal));
        }

        if (from.HasValue)
            lines = lines.Where(l => TryGetTimestamp(l, out var ts) && ts >= from.Value);

        if (to.HasValue)
            lines = lines.Where(l => TryGetTimestamp(l, out var ts) && ts <= to.Value.AddDays(1));

        // show last 500 lines to keep page fast
        var result = lines.TakeLast(500).ToArray();

        return View(result);
    }

    private static bool TryGetTimestamp(string line, out DateTime ts)
    {
        ts = default;
        if (line.Length < 10) return false;

        // First 23 chars are 'yyyy-MM-dd HH:mm:ss.fff'
        var prefix = line.Length >= 23 ? line[..23] : line;
        return DateTime.TryParse(prefix, out ts);
    }
}
