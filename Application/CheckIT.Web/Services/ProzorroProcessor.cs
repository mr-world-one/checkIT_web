using CheckIT.Web.Models;

namespace CheckIT.Web.Services;

public class ProzorroProcessor
{
    private readonly ProzorroService _prozorroService;
    private readonly IAppLogger _logger;

    public ProzorroProcessor(ProzorroService prozorroService, IAppLogger logger)
    {
        _prozorroService = prozorroService;
        _logger = logger;
    }

    public async Task<List<ComparisonItem>> ProcessTenderAsync(string tenderId, CancellationToken ct = default)
    {
        var items = await _prozorroService.GetContractItemsAsync(tenderId, ct);

        var results = new List<ComparisonItem>();
        if (items == null || items.Count == 0) return results;

        using var promScraper = new PromUaSeleniumScraper(_logger, headless: true);

        foreach (var it in items)
        {
            decimal? marketPrice = null;
            try
            {
                var unitPrice = it.UnitPrice ?? 0m;
                var minFloor = ProductMatchRules.GetMinPriceFloor(it.Name, unitPrice);
                var found = await promScraper.FindProductsAsync(it.Name, 15, ct);

                var ranked = found
                    .Select(p => new
                    {
                        Title = p.Title,
                        Score = TextSimilarity.SimilarityPercent(it.Name, p.Title),
                        Price = PriceParsing.TryParsePrice(p.Price, out var d) ? (decimal?)d : null
                    })
                    .Where(x => x.Price.HasValue && x.Price.Value > 0)
                    .Where(x => x.Price.Value >= minFloor)
                    .Where(x => !ProductMatchRules.IsAccessoryLikely(it.Name, x.Title))
                    .Where(x => ProductMatchRules.ContainsAnyKeyTokens(it.Name, x.Title))
                    .ToList();

                if (ranked.Count == 0)
                {
                    ranked = found
                        .Select(p => new
                        {
                            Title = p.Title,
                            Score = TextSimilarity.SimilarityPercent(it.Name, p.Title),
                            Price = PriceParsing.TryParsePrice(p.Price, out var d) ? (decimal?)d : null
                        })
                        .Where(x => x.Price.HasValue && x.Price.Value > 0)
                        .Where(x => x.Price.Value >= minFloor)
                        .Where(x => !ProductMatchRules.IsAccessoryLikely(it.Name, x.Title))
                        .ToList();
                }

                if (ranked.Count > 0)
                {
                    var best = ranked
                        .OrderByDescending(x => x.Score)
                        .ThenBy(x => unitPrice > 0 ? Math.Abs(x.Price!.Value - unitPrice) / unitPrice : 0m)
                        .First();

                    marketPrice = best.Price!.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Prozorro analysis: scraping failed for '{it.Name}'", ex);
                marketPrice = null;
            }

            results.Add(new ComparisonItem
            {
                Name = it.Name,
                Price = it.UnitPrice,
                MarketPrice = marketPrice
            });
        }

        return results;
    }
}
