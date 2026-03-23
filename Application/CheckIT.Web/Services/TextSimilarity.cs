using System.Text.RegularExpressions;

namespace CheckIT.Web.Services;

public static class TextSimilarity
{
    public static decimal SimilarityPercent(string? a, string? b)
    {
        a = Normalize(a);
        b = Normalize(b);

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0m;

        if (a == b) return 1m;

        // Token-based Jaccard similarity (simple, fast, good for product titles)
        var ta = Tokenize(a);
        var tb = Tokenize(b);
        if (ta.Count == 0 || tb.Count == 0) return 0m;

        var intersection = ta.Intersect(tb).Count();
        var union = ta.Union(tb).Count();

        return union == 0 ? 0m : (decimal)intersection / union;
    }

    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        s = s.ToLowerInvariant();
        s = s.Replace('’', '\'').Replace('`', '\'');

        // Keep only letters/digits and spaces
        s = Regex.Replace(s, "[^a-zà-ÿ³¿º0-9 ]+", " ");
        s = Regex.Replace(s, "\\s+", " ").Trim();

        return s;
    }

    private static HashSet<string> Tokenize(string s)
    {
        var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .ToHashSet(StringComparer.Ordinal);

        return tokens;
    }
}
