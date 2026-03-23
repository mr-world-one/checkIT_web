using CheckIT.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Web.Controllers;

[Authorize]
public class ExcelAnalysisController : Controller
{
    private readonly ExcelProcessingService _excel;
    private readonly IAppLogger _logger;

    public ExcelAnalysisController(ExcelProcessingService excel, IAppLogger logger)
    {
        _excel = excel;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.Warn("Excel upload: empty file");
            ModelState.AddModelError(string.Empty, "Файл порожній або не вибраний");
            return View("Index");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            _logger.Warn($"Excel upload: file too large ({file.Length} bytes)");
            ModelState.AddModelError(string.Empty, "Файл завеликий (макс. 10 MB)");
            return View("Index");
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warn($"Excel upload: invalid extension '{file.FileName}'");
            ModelState.AddModelError(string.Empty, "Невірний формат файлу. Підтримується лише .xlsx");
            return View("Index");
        }

        List<Models.ComparisonItem> items;
        try
        {
            using var stream = file.OpenReadStream();
            items = _excel.ParseExcel(stream);
        }
        catch (Exception ex)
        {
            _logger.Error("Excel upload: parse/read error", ex);
            ModelState.AddModelError(string.Empty, "Помилка читання");
            return View("Index");
        }

        if (items.Count == 0)
        {
            _logger.Warn("Excel upload: no rows parsed");
            ModelState.AddModelError(string.Empty, "Файл порожній");
            return View("Index");
        }

        using var scraper = new PromUaSeleniumScraper(_logger, headless: true);

        foreach (var item in items)
        {
            try
            {
                var query = item.Name ?? string.Empty;
                var tenderPrice = item.Price ?? 0m;
                var minFloor = ProductMatchRules.GetMinPriceFloor(query, tenderPrice);

                var found = await scraper.FindProductsAsync(query, 15, CancellationToken.None);

                var ranked = found
                    .Select(p => new
                    {
                        Title = p.Title,
                        Score = TextSimilarity.SimilarityPercent(query, p.Title),
                        Price = PriceParsing.TryParsePrice(p.Price, out var d) ? (decimal?)d : null
                    })
                    .Where(x => x.Price.HasValue && x.Price.Value > 0)
                    .Where(x => x.Price.Value >= minFloor)
                    .Where(x => !ProductMatchRules.IsAccessoryLikely(query, x.Title))
                    .Where(x => ProductMatchRules.ContainsAnyKeyTokens(query, x.Title))
                    .ToList();

                if (ranked.Count == 0)
                {
                    // fallback: relax key token requirement first
                    ranked = found
                        .Select(p => new
                        {
                            Title = p.Title,
                            Score = TextSimilarity.SimilarityPercent(query, p.Title),
                            Price = PriceParsing.TryParsePrice(p.Price, out var d) ? (decimal?)d : null
                        })
                        .Where(x => x.Price.HasValue && x.Price.Value > 0)
                        .Where(x => x.Price.Value >= minFloor)
                        .Where(x => !ProductMatchRules.IsAccessoryLikely(query, x.Title))
                        .ToList();
                }

                if (ranked.Count == 0)
                {
                    item.MarketPrice = null;
                    continue;
                }

                // Prefer high similarity; within same similarity bucket, prefer price closer to tender price
                var best = ranked
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => tenderPrice > 0 ? Math.Abs(x.Price!.Value - tenderPrice) / tenderPrice : 0m)
                    .First();

                item.MarketPrice = best.Price!.Value;
            }
            catch (Exception ex)
            {
                _logger.Error($"Excel analysis: scraping failed for '{item.Name}'", ex);
                item.MarketPrice = null;
            }
        }

        return View("Results", items);
    }
}
