using CheckIT.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CheckIT.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestAuthScheme = "TestAuth";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace AppDbContext with in-memory for integration tests.
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("CheckIT_TestDb_" + Guid.NewGuid()));

            // Test authentication scheme so we can access [Authorize] endpoints.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthScheme;
                options.DefaultChallengeScheme = TestAuthScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, _ => { });
        });
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // By default authenticate as Admin.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test"),
                new Claim(ClaimTypes.Name, "test"),
                new Claim(ClaimTypes.Role, "Admin"),
            };

            var identity = new ClaimsIdentity(claims, TestAuthScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestAuthScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
