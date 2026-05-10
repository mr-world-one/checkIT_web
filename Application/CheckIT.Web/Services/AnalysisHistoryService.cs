using System.Text.Json;
using CheckIT.Web.Data;
using CheckIT.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckIT.Web.Services;

public class AnalysisHistoryService
{
    private readonly AppDbContext _db;

    public AnalysisHistoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(string userId, string type, string source, List<ComparisonItem> items)
    {
        var entry = new AnalysisHistory
        {
            UserId = userId,
            Type = type,
            Source = source,
            ItemsCount = items.Count,
            ItemsJson = JsonSerializer.Serialize(items),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.AnalysisHistories.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AnalysisHistory>> GetUserHistoryAsync(string userId)
    {
        return await _db.AnalysisHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public List<ComparisonItem> DeserializeItems(string json)
    {
        return JsonSerializer.Deserialize<List<ComparisonItem>>(json) ?? [];
    }
}