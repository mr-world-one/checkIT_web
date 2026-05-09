using CheckIT.Tests.TestDoubles;
using CheckIT.Web.Controllers;
using CheckIT.Web.Models;
using CheckIT.Web.Services;
using CheckIT.Web.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace CheckIT.Tests.Controllers;

public class UnblockControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static UnblockController CreateController(
        Mock<UserManager<ApplicationUser>> userManager,
        UnblockRequestService service,
        ClaimsPrincipal? user = null)
    {
        var controller = new UnblockController(userManager.Object, service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        controller.TempData = new FakeTempDataDictionary();
        return controller;
    }

    [Fact]
    public async Task Public_Get_ReturnsViewWithVm_Positive()
    {
        var userManager = CreateUserManager();
        var db = new Mock<CheckIT.Web.Data.AppDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<CheckIT.Web.Data.AppDbContext>());
        var svc = new UnblockRequestService(db.Object, userManager.Object);

        var controller = CreateController(userManager, svc);

        var result = controller.Public("u@e");

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<PublicUnblockRequestVm>()
            .Which.Email.Should().Be("u@e");
    }

    [Fact]
    public async Task Public_Post_WhenModelInvalid_ReturnsViewSameModel_Negative()
    {
        var userManager = CreateUserManager();
        var db = new Mock<CheckIT.Web.Data.AppDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<CheckIT.Web.Data.AppDbContext>());
        var svc = new UnblockRequestService(db.Object, userManager.Object);

        var controller = CreateController(userManager, svc);
        controller.ModelState.AddModelError("Email", "Required");

        var vm = new PublicUnblockRequestVm { Email = "bad", Message = "123" };

        var result = await controller.Public(vm, CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeSameAs(vm);
    }

    [Fact]
    public async Task Public_Post_WhenUserNotFound_ReturnsViewWithModelError_Negative()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync("u@e")).ReturnsAsync((ApplicationUser?)null);

        var db = new Mock<CheckIT.Web.Data.AppDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<CheckIT.Web.Data.AppDbContext>());
        var svc = new UnblockRequestService(db.Object, userManager.Object);

        var controller = CreateController(userManager, svc);

        var result = await controller.Public(new PublicUnblockRequestVm { Email = "u@e", Message = "цей текст достатньо довгий" }, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Public_Post_WhenUserNotBlocked_ReturnsViewWithModelError_Negative()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync("u@e")).ReturnsAsync(new ApplicationUser { Id = "1", Email = "u@e", IsBlocked = false });

        var db = new Mock<CheckIT.Web.Data.AppDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<CheckIT.Web.Data.AppDbContext>());
        var svc = new UnblockRequestService(db.Object, userManager.Object);

        var controller = CreateController(userManager, svc);

        var result = await controller.Public(new PublicUnblockRequestVm { Email = "u@e", Message = "цей текст достатньо довгий" }, CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }
}
