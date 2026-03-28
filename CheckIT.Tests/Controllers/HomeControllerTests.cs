using CheckIT.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CheckIT.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsView()
    {
        var controller = new HomeController();

        var result = controller.Index();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsView()
    {
        var controller = new HomeController();

        var result = controller.Error();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void About_ReturnsView()
    {
        var controller = new HomeController();

        var result = controller.About();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var controller = new HomeController();

        var result = controller.Privacy();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Index_DoesNotSpecifyViewName_NegativeStyle()
    {
        var controller = new HomeController();

        var result = controller.Index();

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
    }

    [Fact]
    public void About_DoesNotSpecifyViewName_NegativeStyle()
    {
        var controller = new HomeController();

        var result = controller.About();

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
    }

    [Fact]
    public void Privacy_DoesNotSpecifyViewName_NegativeStyle()
    {
        var controller = new HomeController();

        var result = controller.Privacy();

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
    }

    [Fact]
    public void Error_DoesNotSpecifyViewName_NegativeStyle()
    {
        var controller = new HomeController();

        var result = controller.Error();

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNull();
    }
}
