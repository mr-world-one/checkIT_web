using CheckIT.Web.Services;
using FluentAssertions;

namespace CheckIT.Tests.Services;

public class ProductMatchRulesTests
{
    [Fact]
    public void GetMinPriceFloor_UsesTenderPrice_WhenNotCheapGoods_Positive()
    {
        ProductMatchRules.GetMinPriceFloor("Laptop Dell 5510", 1000m)
            .Should().Be(200m); // 20% of 1000
    }

    [Fact]
    public void GetMinPriceFloor_UsesMinimum50_WhenTenderPriceSmall_Positive()
    {
        ProductMatchRules.GetMinPriceFloor("Laptop", 100m)
            .Should().Be(50m);
    }

    [Fact]
    public void ContainsAnyKeyTokens_ReturnsTrue_WhenQueryHasNoKeyTokens_Positive()
    {
        ProductMatchRules.ContainsAnyKeyTokens("a b", "whatever").Should().BeTrue();
    }

    [Fact]
    public void ContainsAnyKeyTokens_ReturnsFalse_WhenNoKeyTokensPresentInTitle_Negative()
    {
        ProductMatchRules.ContainsAnyKeyTokens("dell 5510 i7 16gb", "acer laptop")
            .Should().BeFalse();
    }
}
