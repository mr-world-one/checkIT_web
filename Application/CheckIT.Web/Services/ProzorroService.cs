using System.Net;
using System.Text.Json;
using CheckIT.Web.Models;

namespace CheckIT.Web.Services;

public class ProzorroService
{
    private readonly HttpClient _http;

    public ProzorroService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { BaseAddress = new Uri("https://public-api.prozorro.gov.ua/api/2.5/") };
    }

    public async Task<List<ProzorroItem>> GetContractItemsAsync(string contractId, CancellationToken ct = default)
    {
        JsonElement data = await GetTenderOrContractDataAsync(contractId, ct);

        var items = new List<ProzorroItem>();

        if (data.TryGetProperty("items", out var itemsJson) && itemsJson.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsJson.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();

                var result = new ProzorroItem();

                if (item.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String)
                    result.Name = descEl.GetString() ?? "";

                if (item.TryGetProperty("quantity", out var qtyEl) && qtyEl.ValueKind == JsonValueKind.Number)
                    result.Quantity = qtyEl.GetDecimal();

                if (item.TryGetProperty("unit", out var unitEl) &&
                    unitEl.ValueKind == JsonValueKind.Object &&
                    unitEl.TryGetProperty("name", out var unitNameEl) &&
                    unitNameEl.ValueKind == JsonValueKind.String)
                    result.UnitName = unitNameEl.GetString() ?? "";

                if (item.TryGetProperty("unit", out unitEl) &&
                    unitEl.ValueKind == JsonValueKind.Object &&
                    unitEl.TryGetProperty("value", out var valueEl) &&
                    valueEl.ValueKind == JsonValueKind.Object &&
                    valueEl.TryGetProperty("amount", out var amountEl) &&
                    amountEl.ValueKind == JsonValueKind.Number)
                    result.UnitPrice = amountEl.GetDecimal();

                if (item.TryGetProperty("value", out var itemValueEl) &&
                    itemValueEl.ValueKind == JsonValueKind.Object &&
                    itemValueEl.TryGetProperty("amount", out var itemTotalEl) &&
                    itemTotalEl.ValueKind == JsonValueKind.Number)
                    result.TotalPrice = itemTotalEl.GetDecimal();

                items.Add(result);
            }
        }

        if (items.Count == 1 && (!items[0].UnitPrice.HasValue || items[0].UnitPrice == 0))
        {
            decimal? totalContract = null;

            if (data.TryGetProperty("value", out var contractValueEl) &&
                contractValueEl.ValueKind == JsonValueKind.Object &&
                contractValueEl.TryGetProperty("amount", out var totalAmountEl) &&
                totalAmountEl.ValueKind == JsonValueKind.Number)
                totalContract = totalAmountEl.GetDecimal();

            if (totalContract.HasValue && items[0].Quantity > 0)
            {
                var t = items[0];
                t.UnitPrice = totalContract.Value / t.Quantity;
                t.TotalPrice = totalContract;
            }
        }

        return items;
    }

    private async Task<JsonElement> GetTenderOrContractDataAsync(string id, CancellationToken ct)
    {
        var resp = await _http.GetAsync($"tenders/{id}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            resp = await _http.GetAsync($"contracts/{id}", ct);

        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            throw new Exception($"Prozorro API error: {resp.StatusCode}\n{text}");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("data", out var data))
            throw new Exception("Некоректна відповідь Prozorro (немає поля 'data').");

        return data.Clone();
    }
}
