using CheckIT.Tests.TestDoubles;
using CheckIT.Web.Controllers;
using CheckIT.Web.Models;
using CheckIT.Web.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace CheckIT.Tests.Controllers;

public class ProfileControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static ProfileController CreateController(Mock<UserManager<ApplicationUser>> userManager, ClaimsPrincipal? user = null)
    {
        var controller = new ProfileController(userManager.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        controller.TempData = new FakeTempDataDictionary();
        return controller;
    }

    [Fact]
    public async Task Index_Get_WhenUserNotFound_ReturnsChallenge_Negative()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager);

        var result = await controller.Index();

        result.Should().BeOfType<ChallengeResult>();
    }

    [Fact]
    public async Task Index_Get_WhenUserFound_ReturnsViewWithModel_Positive()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Email = "u@e", FullName = "Name" });

        var controller = CreateController(userManager);

        var result = await controller.Index();

        var view = result.Should().BeOfType<ViewResult>().Which;
        var model = view.Model.Should().BeOfType<ProfileViewModel>().Which;
        model.Email.Should().Be("u@e");
        model.FullName.Should().Be("Name");
    }

    [Fact]
    public async Task Index_Post_WhenModelInvalid_ReturnsViewSameModel_Negative()
    {
        var userManager = CreateUserManager();
        var controller = CreateController(userManager);

        controller.ModelState.AddModelError("FullName", "Required");
        var model = new ProfileViewModel { Email = "u@e", FullName = "" };

        var result = await controller.Index(model);

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeSameAs(model);
        userManager.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Index_Post_WhenUserNotFound_ReturnsChallenge_Negative()
    {
        var userManager = CreateUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager);

        var result = await controller.Index(new ProfileViewModel { FullName = "New" });

        result.Should().BeOfType<ChallengeResult>();
        userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Index_Post_WhenValid_UpdatesFullNameTrimmed_SetsTempDataAndRedirects_Positive()
    {
        var appUser = new ApplicationUser { Email = "u@e", FullName = "Old" };

        var userManager = CreateUserManager();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);
        userManager.Setup(m => m.UpdateAsync(appUser)).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(userManager);

        var result = await controller.Index(new ProfileViewModel { FullName = "  New Name  " });

        appUser.FullName.Should().Be("New Name");
        controller.TempData.Should().ContainKey("Success");
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be(nameof(ProfileController.Index));
        userManager.Verify(m => m.UpdateAsync(appUser), Times.Once);
    }
}
