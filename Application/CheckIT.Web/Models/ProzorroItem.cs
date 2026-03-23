namespace CheckIT.Web.Models;

public class ProzorroItem
{
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UnitName { get; set; } = "";
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
}
