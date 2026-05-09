namespace CheckIT.Web.Models;

public class AnalysisHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; 
    public string ItemsJson { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}