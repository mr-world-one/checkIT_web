using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace CheckIT.Tests.Integration;

public class AdminLogsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminLogsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminLogs_FiltersByLevelAndDateRange_WhenAuthorized()
    {
        var root = _factory.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().ContentRootPath;

        var dir = Path.Combine(root, "Logs");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "app.log");

        File.WriteAllLines(path,
        [
            "2025-01-01 10:00:00.000\tINFO\tStart",
            "2025-01-01 12:00:00.000\tWARN\tWarn A",
            "2025-01-02 08:00:00.000\tWARN\tWarn B",
            "2025-01-03 09:00:00.000\tERROR\tErr",
        ]);

        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act: from=2025-01-02 to=2025-01-02 includes only that day; level=warn keeps WARN lines.
        var resp = await client.GetAsync("/Admin/Logs?level=warn&from=2025-01-02&to=2025-01-02");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();

        html.Should().Contain("Warn B");
        html.Should().NotContain("Warn A");
        html.Should().NotContain("Start");
        html.Should().NotContain("Err");
    }

    [Fact]
    public async Task AdminLogs_WhenLogFileMissing_DoesNotRenderAnyLogLines_WhenAuthorized()
    {
        var root = _factory.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().ContentRootPath;

        var path = Path.Combine(root, "Logs", "app.log");
        if (File.Exists(path))
            File.Delete(path);

        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/Admin/Logs");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();

        html.Should().NotContain("\tINFO\t");
        html.Should().NotContain("\tWARN\t");
        html.Should().NotContain("\tERROR\t");
    }
}
