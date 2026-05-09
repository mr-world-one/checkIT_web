namespace CheckIT.Web.Services;

public interface IPromScraper : IDisposable
{
    Task<IReadOnlyList<PromProduct>> FindProductsAsync(string query, int limit, CancellationToken ct);
}

public sealed record PromProduct(string Title, string Price);
