using CheckIT.Web.Services;
using FluentAssertions;

namespace CheckIT.Tests.Services;

public class TextSimilarityTests
{
    [Fact]
    public void SimilarityPercent_IsOne_ForEqualAfterNormalization_Positive()
    {
        var s = TextSimilarity.SimilarityPercent("Dell-5510", "dell 5510");
        s.Should().Be(1m);
    }

    [Fact]
    public void SimilarityPercent_IsZero_WhenEitherSideEmpty_Negative()
    {
        TextSimilarity.SimilarityPercent(null, "abc").Should().Be(0m);
        TextSimilarity.SimilarityPercent("abc", "  ").Should().Be(0m);
    }

    [Fact]
    public void SimilarityPercent_IsSymmetric_PropertyBasedStyle()
    {
        // A small "property" test without extra libs
        var pairs = new[]
        {
            ("Lenovo ThinkPad X1", "ThinkPad X1 Carbon"),
            ("Кабель USB", "USB кабель 2m"),
            ("HP 250 G8", "HP 250"),
        };

        foreach (var (a, b) in pairs)
        {
            TextSimilarity.SimilarityPercent(a, b)
                .Should().Be(TextSimilarity.SimilarityPercent(b, a));
        }
    }
}
