using System.Globalization;
using System.Text.RegularExpressions;
using CheckIT.Web.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CheckIT.Web.Services;

/// <summary>
/// Prom.ua scraper via Selenium WebDriver (Chrome).
/// Searches prom.ua and returns best-effort product list with price.
/// </summary>
public sealed class PromUaSeleniumScraper : IDisposable
{
    private readonly IAppLogger? _logger;
    private readonly bool _headless;
    private readonly int _timeoutSeconds;
    private IWebDriver? _driver;

    public PromUaSeleniumScraper(IAppLogger? logger = null, bool headless = true, int timeoutSeconds = 25)
    {
        _logger = logger;
        _headless = headless;
        _timeoutSeconds = Math.Clamp(timeoutSeconds, 5, 120);
    }

    public Task<IReadOnlyList<ScrapedProduct>> FindProductsAsync(string query, int n, CancellationToken ct)
        => Task.Run(() => FindProductsCore(query, n, ct), ct);

    private IReadOnlyList<ScrapedProduct> FindProductsCore(string query, int n, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || n <= 0)
            return Array.Empty<ScrapedProduct>();

        EnsureDriver();
        ct.ThrowIfCancellationRequested();

        var url = $"https://prom.ua/ua/search?search_term={Uri.EscapeDataString(query)}";
        _logger?.Info($"[Prom.ua Selenium] Navigating: {url}");

        _driver!.Navigate().GoToUrl(url);

        var wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(_timeoutSeconds), TimeSpan.FromMilliseconds(250));

        try
        {
            wait.Until(d =>
            {
                ct.ThrowIfCancellationRequested();
                return GetCards(d).Count > 0;
            });
        }
        catch (WebDriverTimeoutException)
        {
            _logger?.Warn($"[Prom.ua Selenium] Timeout waiting for cards. Title='{SafeTitle()}'");
        }

        var results = new List<ScrapedProduct>();
        var maxTotalAttempts = Math.Max(n * 10, 30);

        for (var i = 0; results.Count < n && i < maxTotalAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            var cards = GetCards(_driver);
            if (i >= cards.Count) break;

            var card = cards[i];

            try
            {
                var title = TryGetText(card, "a[data-qaid='product_name'], a[title]");
                if (string.IsNullOrWhiteSpace(title))
                    title = card.Text?.Split('\n').FirstOrDefault() ?? string.Empty;

                var href = TryGetHref(card, "a[data-qaid='product_name'], a[title], a");

                // Price: try explicit nodes/attributes first.
                var price = TryExtractPrice(card);
                if (price is null || price <= 0)
                    continue;

                results.Add(new ScrapedProduct
                {
                    Source = "Prom.ua",
                    Title = title.Trim(),
                    Url = href,
                    Price = price.Value.ToString("0.00", CultureInfo.InvariantCulture)
                });
            }
            catch (StaleElementReferenceException)
            {
                Thread.Sleep(150);
                i--; // retry same index
            }
        }

        _logger?.Info($"[Prom.ua Selenium] Found {results.Count} products for '{query}'");
        return results;
    }

    private static decimal? TryExtractPrice(IWebElement card)
    {
        // 1) data-qaid price nodes
        var txt = TryGetText(card, "[data-qaid='product_price'], [data-qaid='product_price_uah'], [data-qaid='product-price'], [data-qaid*='price']");
        var d = ExtractUAH(txt);
        if (d > 0) return d;

        // 2) meta tags sometimes exist inside card (itemprop price)
        try
        {
            var meta = card.FindElements(By.CssSelector("meta[itemprop='price']")).FirstOrDefault();
            var content = meta?.GetDomAttribute("content") ?? meta?.GetAttribute("content");
            if (!string.IsNullOrWhiteSpace(content) && decimal.TryParse(content, NumberStyles.Any, CultureInfo.InvariantCulture, out var md) && md > 0)
                return md;
        }
        catch { }

        // 3) attributes on elements
        try
        {
            var el = card.FindElements(By.CssSelector("[data-price], [data-qaid*='price']")).FirstOrDefault();
            var p = el?.GetDomAttribute("data-price") ?? el?.GetAttribute("data-price")
                ?? el?.GetDomAttribute("content") ?? el?.GetAttribute("content")
                ?? el?.GetDomAttribute("value") ?? el?.GetAttribute("value");
            if (!string.IsNullOrWhiteSpace(p) && decimal.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out var ad) && ad > 0)
                return ad;
        }
        catch { }

        // 4) last resort: entire card text
        var t = card.Text;
        var last = ExtractUAH(t);
        return last > 0 ? last : null;
    }

    private static decimal ExtractUAH(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Replace("\u00A0", " ").ToLowerInvariant();

        var matches = Regex.Matches(text, @"(\d{1,3}(?:\s\d{3})*|\d+)(?:[\.,]\d+)?");
        if (matches.Count == 0) return 0;

        decimal best = 0;
        foreach (Match m in matches)
        {
            var raw = m.Value.Replace(" ", "").Replace(',', '.');
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var dd) && dd > best)
                best = dd;
        }

        return best >= 10 ? best : 0;
    }

    private static List<IWebElement> GetCards(ISearchContext driver)
    {
        var cards = driver.FindElements(By.CssSelector("div[data-qaid='product_block'], div[data-qaid='product-item'], div[data-qaid='product-content'], div[data-testid='product-card']")).ToList();
        if (cards.Count != 0) return cards;

        return driver.FindElements(By.CssSelector("a[data-qaid='product_name']"))
            .Select(a => (IWebElement)a.FindElement(By.XPath("./ancestor::div[1]"))).ToList();
    }

    private void EnsureDriver()
    {
        if (_driver != null) return;

        var options = new ChromeOptions();
        options.AddArgument("--lang=uk-UA");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1366,768");

        if (_headless)
            options.AddArgument("--headless=new");

        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        var driverPath = Environment.GetEnvironmentVariable("CHECKIT_CHROMEDRIVER_PATH");

        if (!string.IsNullOrWhiteSpace(driverPath) && File.Exists(driverPath))
        {
            _logger?.Info($"[Selenium] Using chromedriver from '{driverPath}'");
            var service = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(driverPath)!, Path.GetFileName(driverPath));
            service.HideCommandPromptWindow = true;
            _driver = new ChromeDriver(service, options);
        }
        else
        {
            _driver = new ChromeDriver(options);
        }

        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    private string SafeTitle()
    {
        try { return _driver?.Title ?? string.Empty; } catch { return string.Empty; }
    }

    private static string TryGetText(IWebElement root, string css)
    {
        try { return root.FindElements(By.CssSelector(css)).FirstOrDefault()?.Text ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static string? TryGetHref(IWebElement root, string css)
    {
        try
        {
            var el = root.FindElements(By.CssSelector(css)).FirstOrDefault();
            return el?.GetDomAttribute("href") ?? el?.GetAttribute("href");
        }
        catch { return null; }
    }

    public void Dispose()
    {
        try { _driver?.Quit(); } catch { }
        try { _driver?.Dispose(); } catch { }
        _driver = null;
    }
}
