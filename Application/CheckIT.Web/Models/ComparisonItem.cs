namespace CheckIT.Web.Models;

public class ComparisonItem
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }

    /// <summary>
    /// Price from external marketplace (Prom.ua).
    /// </summary>
    public decimal? MarketPrice { get; set; }
}
