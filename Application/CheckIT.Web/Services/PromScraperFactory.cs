namespace CheckIT.Web.Services;

public sealed class PromScraperFactory : IPromScraperFactory
{
    private readonly IAppLogger _logger;

    public PromScraperFactory(IAppLogger logger)
    {
        _logger = logger;
    }

    public IPromScraper Create(bool headless = true) => new PromUaSeleniumScraperAdapter(new PromUaSeleniumScraper(_logger, headless));

    private sealed class PromUaSeleniumScraperAdapter : IPromScraper
    {
        private readonly PromUaSeleniumScraper _inner;

        public PromUaSeleniumScraperAdapter(PromUaSeleniumScraper inner)
        {
            _inner = inner;
        }

        public async Task<IReadOnlyList<PromProduct>> FindProductsAsync(string query, int limit, CancellationToken ct)
        {
            var found = await _inner.FindProductsAsync(query, limit, ct);
            return found.Select(p => new PromProduct(p.Title, p.Price)).ToList();
        }

        public void Dispose() => _inner.Dispose();
    }
}
