using System.Globalization;

namespace CheckIT.Web.Services;

internal static class PriceParsing
{
    public static bool TryParsePrice(string? s, out decimal value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim()
             .Replace(" ", "")
             .Replace("\u00A0", "")
             .Replace(',', '.');

        return decimal.TryParse(
            s,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out value);
    }
}
