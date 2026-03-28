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

public class AccountControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

        var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            contextAccessor.Object,
            userPrincipalFactory.Object,
            options.Object,
            null!, null!, null!);
    }

    private static AccountController CreateController(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<SignInManager<ApplicationUser>> signInManager,
        Mock<IAppLogger> logger,
        ClaimsPrincipal? user = null)
    {
        var controller = new AccountController(userManager.Object, signInManager.Object, logger.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        controller.TempData = new FakeTempDataDictionary();
        return controller;
    }

    [Fact]
    public void Login_Get_WhenAuthenticated_RedirectsHome()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "x") }, "Test"));
        var controller = CreateController(userManager, signInManager, logger, principal);

        var result = controller.Login();

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public void Login_Get_WhenAnonymous_ReturnsViewWithModel()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        var result = controller.Login();

        var view = result.Should().BeOfType<ViewResult>().Which;
        view.Model.Should().BeOfType<LoginViewModel>();
    }

    [Fact]
    public async Task Login_Post_WhenModelInvalid_ReturnsViewSameModel()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);
        controller.ModelState.AddModelError("Email", "Required");

        var model = new LoginViewModel { Email = "a@b.com", Password = "x" };

        var result = await controller.Login(model);

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeSameAs(model);
    }

    [Fact]
    public async Task Login_Post_WhenUserNotFound_ReturnsViewWithModelError()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        var result = await controller.Login(new LoginViewModel { Email = "missing@x.com", Password = "Pass123!" });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_Post_WhenBlocked_ReturnsViewWithModelError()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { UserName = "u", Email = "u@e", IsBlocked = true });

        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        var result = await controller.Login(new LoginViewModel { Email = "u@e", Password = "Pass123!" });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        signInManager.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Login_Post_WhenSuccess_RedirectsHomeAndSetsTempData()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { UserName = "u", Email = "u@e", FullName = "Name", IsBlocked = false });

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(s => s.PasswordSignInAsync("u", It.IsAny<string>(), false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(userManager, signInManager, logger);

        var result = await controller.Login(new LoginViewModel { Email = "u@e", Password = "Pass123!" });

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        controller.TempData.Should().ContainKey("Success");
    }

    [Fact]
    public async Task Register_Get_WhenAuthenticated_RedirectsHome()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "x") }, "Test"));
        var controller = CreateController(userManager, signInManager, logger, principal);

        var result = controller.Register();

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public void Register_Get_WhenAnonymous_ReturnsViewWithModel()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        var result = controller.Register();

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<RegisterViewModel>();
    }

    [Fact]
    public async Task Register_Post_WhenCreateFails_ReturnsViewWithModelErrors()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Fail" }));

        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        var model = new RegisterViewModel { Email = "e@e.com", Password = "Pass123!", Name = "N" };
        var result = await controller.Register(model);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Never);
    }

    [Fact]
    public async Task Register_Post_WhenSuccess_RedirectsHome()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(userManager, signInManager, logger);

        var model = new RegisterViewModel { Email = "e@e.com", Password = "Pass123!", Name = "N" };
        var result = await controller.Register(model);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        controller.TempData.Should().ContainKey("Success");
    }

    [Fact]
    public async Task Logout_Post_SignsOutAndRedirectsHome()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        var logger = new Mock<IAppLogger>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "x") }, "Test"));
        var controller = CreateController(userManager, signInManager, logger, principal);

        var result = await controller.Logout();

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        controller.TempData.Should().ContainKey("Success");
        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public void AccessDenied_Get_ReturnsView()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        controller.AccessDenied().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_WhenLockedOut_ReturnsViewAndAddsModelError_Negative()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { UserName = "u", Email = "u@e", IsBlocked = false });

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(s => s.PasswordSignInAsync("u", It.IsAny<string>(), false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(userManager, signInManager, logger);

        var result = await controller.Login(new LoginViewModel { Email = "u@e", Password = "Pass123!" });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        logger.Verify(l => l.Warn(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Register_Post_WhenModelInvalid_ReturnsViewSameModel_Negative()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);
        controller.ModelState.AddModelError("Email", "Required");

        var model = new RegisterViewModel { Email = "e@e.com", Password = "Pass123!", Name = "N" };

        var result = await controller.Register(model);

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeSameAs(model);
        userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_Post_WhenSignOutThrows_PropagatesException_Negative()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(s => s.SignOutAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        var logger = new Mock<IAppLogger>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "x") }, "Test"));
        var controller = CreateController(userManager, signInManager, logger, principal);

        await FluentActions.Invoking(() => controller.Logout())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Register_Post_WhenPasswordInvalid_AddsPasswordRequirementsMessage()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Too short" }));

        var signInManager = CreateSignInManager(userManager.Object);
        var logger = new Mock<IAppLogger>();

        var controller = CreateController(userManager, signInManager, logger);

        var model = new RegisterViewModel { Email = "e@e.com", Password = "bad", Name = "N" };

        var result = await controller.Register(model);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ContainsKey(nameof(RegisterViewModel.Password)).Should().BeTrue();
        controller.ModelState[nameof(RegisterViewModel.Password)]!.Errors
            .Should().Contain(e => e.ErrorMessage.Contains("мінімум 8", StringComparison.OrdinalIgnoreCase));
    }
}
