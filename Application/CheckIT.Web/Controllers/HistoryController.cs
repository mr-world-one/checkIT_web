using CheckIT.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CheckIT.Web.Controllers;

[Authorize]
public class HistoryController : Controller
{
    private readonly AnalysisHistoryService _historyService;

    public HistoryController(AnalysisHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var history = await _historyService.GetUserHistoryAsync(userId);
        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var history = await _historyService.GetUserHistoryAsync(userId);
        var entry = history.FirstOrDefault(h => h.Id == id);

        if (entry == null)
            return NotFound();

        var items = _historyService.DeserializeItems(entry.ItemsJson);
        ViewBag.Entry = entry;
        return View("Details", items);
    }
}