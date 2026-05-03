using CheckIT.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Web.Controllers;

[Authorize]
public class ProzorroAnalysisController : Controller
{
    private readonly ProzorroProcessor _processor;
    private readonly IAppLogger _logger;
    private readonly AnalysisHistoryService _historyService;

    public ProzorroAnalysisController(ProzorroProcessor processor, IAppLogger logger, AnalysisHistoryService historyService) // ← додай параметр
    {
        _processor = processor;
        _logger = logger;
        _historyService = historyService; // ← додай
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(string? tenderId)
    {
        if (string.IsNullOrWhiteSpace(tenderId))
        {
            ModelState.AddModelError(string.Empty, "Введіть ID тендеру / контракту");
            return View("Index");
        }

        try
        {
            var results = await _processor.ProcessTenderAsync(tenderId.Trim());
            if (results.Count == 0)
            {
                _logger.Warn($"Prozorro analysis: no items for id '{tenderId}'");
                ModelState.AddModelError(string.Empty, "Тендер без цін або позицій");
                return View("Index");
            }

            // ← додай ці два рядки
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _historyService.SaveAsync(userId, "Prozorro", tenderId.Trim(), results);

            return View("Results", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Prozorro analysis: failed for id '{tenderId}'", ex);
            ModelState.AddModelError(string.Empty, "Помилка з'єднання");
            return View("Index");
        }
    }
}