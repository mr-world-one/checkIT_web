using FluentAssertions;
using System.Net;

namespace CheckIT.Tests.Integration;

public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/About")]
    public async Task PublicPages_Return200(string url)
    {
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.GetAsync(url);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthorizedPage_WhenAuthenticated_Return200()
    {
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/ExcelAnalysis");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
