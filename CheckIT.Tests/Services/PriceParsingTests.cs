using CheckIT.Web.Services;
using FluentAssertions;

namespace CheckIT.Tests.Services;

public class PriceParsingTests
{
    [Theory]
    [InlineData("1 234,56", 1234.56)]
    [InlineData("\u00A01\u00A0234,56", 1234.56)]
    [InlineData("-10.5", -10.5)]
    public void TryParsePrice_Parses_CommonFormats_Positive(string input, decimal expected)
    {
        PriceParsing.TryParsePrice(input, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UAH 10")]
    public void TryParsePrice_ReturnsFalse_ForInvalidInputs_Negative(string? input)
    {
        PriceParsing.TryParsePrice(input, out var value).Should().BeFalse();
        value.Should().Be(0m);
    }
}
