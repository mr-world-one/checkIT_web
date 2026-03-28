using CheckIT.Web.Controllers;
using CheckIT.Web.Models;
using CheckIT.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CheckIT.Tests.Controllers;

public class ProzorroAnalysisControllerTests
{
    private static ProzorroAnalysisController CreateController(Mock<ProzorroProcessor> processor, Mock<IAppLogger> logger)
    {
        var controller = new ProzorroAnalysisController(processor.Object, logger.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public void Index_ReturnsView()
    {
        var processor = new Mock<ProzorroProcessor>(null!, null!, null!);
        var logger = new Mock<IAppLogger>();
        var controller = CreateController(processor, logger);

        controller.Index().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Analyze_WhenTenderIdMissing_ReturnsIndexWithModelError_Negative()
    {
        var processor = new Mock<ProzorroProcessor>(null!, null!, null!);
        var logger = new Mock<IAppLogger>();
        var controller = CreateController(processor, logger);

        var result = await controller.Analyze(null);

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        processor.Verify(p => p.ProcessTenderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Analyze_WhenProcessorReturnsItems_ReturnsResultsView_Positive()
    {
        var processor = new Mock<ProzorroProcessor>(null!, null!, null!);
        processor.Setup(p => p.ProcessTenderAsync("UA-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComparisonItem> { new() { Name = "n" } });

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(processor, logger);

        var result = await controller.Analyze(" UA-1 ");

        var view = result.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().Be("Results");
        view.Model.Should().BeAssignableTo<IEnumerable<ComparisonItem>>();
    }

    [Fact]
    public async Task Analyze_WhenProcessorReturnsEmpty_ReturnsIndexWithError_Negative()
    {
        var processor = new Mock<ProzorroProcessor>(null!, null!, null!);
        processor.Setup(p => p.ProcessTenderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(processor, logger);

        var result = await controller.Analyze("UA-1");

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Analyze_WhenProcessorThrows_ReturnsIndexWithError_Negative()
    {
        var processor = new Mock<ProzorroProcessor>(null!, null!, null!);
        processor.Setup(p => p.ProcessTenderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(processor, logger);

        var result = await controller.Analyze("UA-1");

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        logger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }
}
