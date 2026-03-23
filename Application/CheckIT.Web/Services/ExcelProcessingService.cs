using CheckIT.Web.Models;
using ClosedXML.Excel;

namespace CheckIT.Web.Services;

public class ExcelProcessingService
{
    public List<ComparisonItem> ParseExcel(Stream fileStream)
    {
        var items = new List<ComparisonItem>();

        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheets.First();

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString();
            if (string.IsNullOrWhiteSpace(name)) continue;

            decimal? price = null;
            var priceText = row.Cell(2).GetString().Replace(',', '.');

            if (decimal.TryParse(priceText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                price = parsed;

            items.Add(new ComparisonItem { Name = name.Trim(), Price = price });
        }

        return items;
    }
}
