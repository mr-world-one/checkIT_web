using CheckIT.Web.Data;
using CheckIT.Web.Infrastructure;
using CheckIT.Web.Models;
using CheckIT.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Azure App Service: persist DataProtection keys so cookies/antiforgery survive restarts.
// Use a writable local path for dev/test/CI; App Service uses /home which is persisted.
var dpKeysPath = builder.Environment.IsProduction()
    ? "/home/DataProtection-Keys"
    : Path.Combine(builder.Environment.ContentRootPath, ".dpkeys");

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetApplicationName("CheckIT");

builder.Services.AddHealthChecks();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = false;
        options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;

        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".CheckIT.Auth";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<UnblockRequestService>();
builder.Services.AddScoped<ExcelProcessingService>();
builder.Services.AddSingleton<ProzorroService>();
builder.Services.AddScoped<ProzorroProcessor>();

builder.Services.AddScoped<IPromScraperFactory, PromScraperFactory>();

var logDir = Path.Combine(builder.Environment.ContentRootPath, "Logs");
builder.Services.AddSingleton<IAppLogger>(_ => new FileAppLogger(Path.Combine(logDir, "app.log")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program { }
