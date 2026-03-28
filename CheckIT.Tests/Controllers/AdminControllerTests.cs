using CheckIT.Web.Controllers;
using CheckIT.Web.Models;
using CheckIT.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Linq.Expressions;

namespace CheckIT.Tests.Controllers;

public class AdminControllerTests
{
    private sealed class TestUserDbSet : IQueryable<ApplicationUser>
    {
        private readonly IQueryable<ApplicationUser> _inner;
        public TestUserDbSet(IEnumerable<ApplicationUser> users) => _inner = users.AsQueryable();
        public Type ElementType => _inner.ElementType;
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => _inner.Provider;
        public IEnumerator<ApplicationUser> GetEnumerator() => _inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerWithUsers(List<ApplicationUser> users)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        mgr.Setup(m => m.Users).Returns(new TestUserDbSet(users));
        return mgr;
    }

    private static AdminController CreateController(AdminService admin, string contentRoot)
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(contentRoot);

        var controller = new AdminController(admin, env.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.TempData = new CheckIT.Tests.TestDoubles.FakeTempDataDictionary();
        return controller;
    }

    private sealed class AdminServiceStub : AdminService
    {
        private readonly Func<Task<List<ApplicationUser>>> _getAll;
        private readonly Func<string, bool, Task>? _setBlocked;
        private readonly Func<string, Task>? _delete;

        public AdminServiceStub(
            UserManager<ApplicationUser> userManager,
            Func<Task<List<ApplicationUser>>> getAll,
            Func<string, bool, Task>? setBlocked = null,
            Func<string, Task>? delete = null)
            : base(userManager)
        {
            _getAll = getAll;
            _setBlocked = setBlocked;
            _delete = delete;
        }

        public override Task<List<ApplicationUser>> GetAllUsersAsync() => _getAll();
        public override Task SetBlockedAsync(string userId, bool blocked) => _setBlocked?.Invoke(userId, blocked) ?? Task.CompletedTask;
        public override Task DeleteUserAsync(string userId) => _delete?.Invoke(userId) ?? Task.CompletedTask;
    }

    [Fact]
    public async Task Dashboard_WhenUsersReturned_SetsViewBagsAndReturnsView()
    {
        var users = new List<ApplicationUser>
        {
            new() { IsBlocked = false },
            new() { IsBlocked = true },
        };

        var userManager = CreateUserManagerWithUsers(users);
        var admin = new AdminServiceStub(userManager.Object, () => Task.FromResult(users));
        var controller = CreateController(admin, Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N")));

        var result = await controller.Dashboard();

        result.Should().BeOfType<ViewResult>();
        ((int)controller.ViewBag.TotalUsers).Should().Be(2);
        ((int)controller.ViewBag.BlockedUsers).Should().Be(1);
    }

    [Fact]
    public async Task Users_ReturnsViewWithUsersModel()
    {
        var list = new List<ApplicationUser> { new() { Email = "a@a" } };
        var userManager = CreateUserManagerWithUsers(list);
        var admin = new AdminServiceStub(userManager.Object, () => Task.FromResult(list));

        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var controller = CreateController(admin, root);

        var result = await controller.Users();

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeSameAs(list);
    }

    [Fact]
    public async Task SetBlocked_Post_RedirectsToUsersAndSetsTempData_Positive()
    {
        var userManager = CreateUserManagerWithUsers([]);
        var called = false;
        var admin = new AdminServiceStub(
            userManager.Object,
            getAll: () => Task.FromResult(new List<ApplicationUser>()),
            setBlocked: (id, b) => { called = true; return Task.CompletedTask; });

        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var controller = CreateController(admin, root);

        var result = await controller.SetBlocked("1", true);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be(nameof(AdminController.Users));
        controller.TempData.Should().ContainKey("Success");
        called.Should().BeTrue();
    }

    [Fact]
    public async Task SetBlocked_Post_WhenServiceThrows_Throws_Negative()
    {
        var userManager = CreateUserManagerWithUsers([]);
        var admin = new AdminServiceStub(
            userManager.Object,
            getAll: () => Task.FromResult(new List<ApplicationUser>()),
            setBlocked: (_, _) => throw new InvalidOperationException("boom"));

        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var controller = CreateController(admin, root);

        await FluentActions.Invoking(() => controller.SetBlocked("1", true))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_Post_RedirectsToUsersAndSetsTempData_Positive()
    {
        var userManager = CreateUserManagerWithUsers([]);
        var called = false;
        var admin = new AdminServiceStub(
            userManager.Object,
            getAll: () => Task.FromResult(new List<ApplicationUser>()),
            delete: _ => { called = true; return Task.CompletedTask; });

        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var controller = CreateController(admin, root);

        var result = await controller.Delete("1");

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be(nameof(AdminController.Users));
        controller.TempData.Should().ContainKey("Success");
        called.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Post_WhenServiceThrows_Throws_Negative()
    {
        var userManager = CreateUserManagerWithUsers([]);
        var admin = new AdminServiceStub(
            userManager.Object,
            getAll: () => Task.FromResult(new List<ApplicationUser>()),
            delete: _ => throw new Exception("boom"));

        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var controller = CreateController(admin, root);

        await FluentActions.Invoking(() => controller.Delete("1"))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void Logs_WhenFileMissing_ReturnsEmptyStringsModelAndSetsMessage()
    {
        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var userManager = CreateUserManagerWithUsers([]);
        var admin = new AdminService(userManager.Object);
        var controller = CreateController(admin, root);

        var logPath = Path.Combine(root, "Logs", "app.log");
        if (File.Exists(logPath))
            File.Delete(logPath);

        var result = controller.Logs();

        var view = result.Should().BeOfType<ViewResult>().Which;
        ((IEnumerable<string>)view.Model!).Should().BeEmpty();
        ((string)controller.ViewBag.Message).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Logs_WhenFileExists_FiltersByLevel_Positive()
    {
        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var userManager = CreateUserManagerWithUsers([]);
        var admin = new AdminService(userManager.Object);
        var controller = CreateController(admin, root);

        var dir = Path.Combine(root, "Logs");
        Directory.CreateDirectory(dir);
        var logPath = Path.Combine(dir, "app.log");
        File.WriteAllLines(logPath,
        [
            "2025-01-01 10:00:00.000\tINFO\tHello",
            "2025-01-01 10:00:01.000\tWARN\tWarn",
        ]);

        var result = controller.Logs(level: "warn");

        var view = result.Should().BeOfType<ViewResult>().Which;
        var lines = ((IEnumerable<string>)view.Model!).ToArray();
        lines.Should().ContainSingle(l => l.Contains("\tWARN\t"));
    }

    [Fact]
    public void Logs_WhenReadLinesThrows_ReturnsEmptyAndSetsMessage_Negative()
    {
        var root = Path.Combine(Path.GetTempPath(), "CheckIT.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var userManager = CreateUserManagerWithUsers([]);
        var admin = new AdminService(userManager.Object);
        var controller = CreateController(admin, root);

        var dir = Path.Combine(root, "Logs");
        Directory.CreateDirectory(dir);
        var logPath = Path.Combine(dir, "app.log");

        if (File.Exists(logPath)) File.Delete(logPath);
        if (Directory.Exists(logPath)) Directory.Delete(logPath, recursive: true);
        Directory.CreateDirectory(logPath);

        var result = controller.Logs();

        var view = result.Should().BeOfType<ViewResult>().Which;
        ((IEnumerable<string>)view.Model!).Should().BeEmpty();
        ((string)controller.ViewBag.Message).Should().NotBeNullOrWhiteSpace();
    }
}
