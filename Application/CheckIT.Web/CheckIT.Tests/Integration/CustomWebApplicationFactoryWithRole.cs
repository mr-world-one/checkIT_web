using CheckIT.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CheckIT.Tests.Integration;

/// <summary>
/// Test factory that allows controlling authenticated role and bypassing antiforgery in Testing.
/// </summary>
public sealed class CustomWebApplicationFactoryWithRole : WebApplicationFactory<Program>
{
    public const string TestAuthScheme = "TestAuth";

    private readonly string[] _roles;
    private readonly string _userId;

    public CustomWebApplicationFactoryWithRole(string userId = "test", params string[] roles)
    {
        _userId = userId;
        _roles = roles.Length == 0 ? ["User"] : roles;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("CheckIT_TestDb_" + Guid.NewGuid()));

            services.AddControllersWithViews(o =>
            {
                // In tests we don't want to deal with antiforgery tokens.
                o.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthScheme;
                options.DefaultChallengeScheme = TestAuthScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, _ => { });

            services.AddSingleton(new TestAuthState(_userId, _roles));
        });
    }

    private sealed record TestAuthState(string UserId, string[] Roles);

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly TestAuthState _state;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            TestAuthState state)
            : base(options, logger, encoder)
        {
            _state = state;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _state.UserId),
                new(ClaimTypes.Name, _state.UserId),
            };

            foreach (var r in _state.Roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            var identity = new ClaimsIdentity(claims, TestAuthScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestAuthScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
