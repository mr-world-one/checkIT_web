using System.Text.RegularExpressions;

namespace CheckIT.Web.Services;

public static class ProductMatchRules
{
    private static readonly string[] AccessoryBadWords =
    [
        "чохол", "сумка", "кейс", "наклад", "плівк", "скло", "захисн", "клавіатур", "миша", "мишк",
        "кабель", "перехідник", "адаптер", "заряд", "блок живлення", "живлення", "кронштейн",
        "картридж", "тонер", "папір", "стікер", "наклейк", "тримач", "підставк", "аксесуар",
        "ремін", "панель", "кришка", "шлейф", "материн", "плата", "корпус", "вентилятор",
        "комплектуюч", "запчаст", "контролер", "роз'єм", "роз\"єм"
    ];

    // categories where cheap prices are expected (so we should not apply high min-price)
    private static readonly string[] CheapGoodsHints =
    [
        "мишка", "миша", "m170", "m185", "клавіат", "кабель", "перехідник", "кетчуп", "соус", "масло",
        "чай", "кава", "цукор", "круп", "печиво", "шоколад"
    ];

    public static bool IsAccessoryLikely(string query, string title)
    {
        var q = Normalize(query);
        var t = Normalize(title);

        // if query itself contains accessory word - do not exclude
        if (AccessoryBadWords.Any(w => q.Contains(w, StringComparison.Ordinal)))
            return false;

        return AccessoryBadWords.Any(w => t.Contains(w, StringComparison.Ordinal));
    }

    public static decimal GetMinPriceFloor(string query, decimal tenderPrice)
    {
        var q = Normalize(query);

        // If it looks like cheap goods, allow low prices
        if (CheapGoodsHints.Any(h => q.Contains(h, StringComparison.Ordinal)))
            return 10m;

        // Otherwise: dynamic floor based on tender price
        if (tenderPrice > 0)
        {
            // accept not less than 20% of tender price, but at least 50 UAH
            return Math.Max(50m, tenderPrice * 0.20m);
        }

        return 50m;
    }

    public static bool ContainsAnyKeyTokens(string query, string title)
    {
        var qTokens = KeyTokens(query);
        if (qTokens.Count == 0) return true;

        var t = Normalize(title);
        // require at least half of key tokens to be present
        var present = qTokens.Count(tok => t.Contains(tok, StringComparison.Ordinal));
        return present >= Math.Max(1, qTokens.Count / 2);
    }

    private static HashSet<string> KeyTokens(string s)
    {
        s = Normalize(s);
        if (string.IsNullOrWhiteSpace(s)) return [];

        // keep model-like tokens: letters+digits, or 3+ letters, or 2+ digits
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in parts)
        {
            if (p.Length < 3) continue;
            if (Regex.IsMatch(p, @"[a-zа-яіїє]+\d+|\d+[a-zа-яіїє]+", RegexOptions.IgnoreCase))
                set.Add(p);
            else if (Regex.IsMatch(p, @"\d{2,}", RegexOptions.IgnoreCase))
                set.Add(p);
            else if (Regex.IsMatch(p, @"[a-zа-яіїє]{3,}", RegexOptions.IgnoreCase))
                set.Add(p);
        }

        // remove very generic words that often appear everywhere
        set.Remove("ноутбук");
        set.Remove("монітор");
        set.Remove("мишка");
        set.Remove("миша");
        set.Remove("холодильник");

        return set;
    }

    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.ToLowerInvariant();
        s = Regex.Replace(s, "[^a-zа-яіїє0-9 ]+", " ");
        s = Regex.Replace(s, "\\s+", " ").Trim();
        return s;
    }
}
