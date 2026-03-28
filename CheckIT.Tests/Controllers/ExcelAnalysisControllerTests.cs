using CheckIT.Web.Controllers;
using CheckIT.Web.Models;
using CheckIT.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CheckIT.Tests.Controllers;

public class ExcelAnalysisControllerTests
{
    private static ExcelAnalysisController CreateController(
        Mock<ExcelProcessingService> excel,
        Mock<IAppLogger> logger,
        Mock<IPromScraperFactory>? scraperFactory = null)
    {
        scraperFactory ??= new Mock<IPromScraperFactory>();
        var controller = new ExcelAnalysisController(excel.Object, logger.Object, scraperFactory.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public void Index_ReturnsView()
    {
        var excel = new Mock<ExcelProcessingService>();
        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger);

        controller.Index().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Upload_WhenFileNull_ReturnsIndexWithError_Negative()
    {
        var excel = new Mock<ExcelProcessingService>();
        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger);

        var result = await controller.Upload(null);

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Upload_WhenExtensionInvalid_ReturnsIndexWithError_Negative()
    {
        var excel = new Mock<ExcelProcessingService>();
        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger);

        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(content, 0, content.Length, "file", "a.txt");

        var result = await controller.Upload(file);

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Upload_WhenParseThrows_ReturnsIndexWithError_Negative()
    {
        var excel = new Mock<ExcelProcessingService>();
        excel.Setup(e => e.ParseExcel(It.IsAny<Stream>())).Throws(new Exception("boom"));

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger);

        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(content, 0, content.Length, "file", "a.xlsx");

        var result = await controller.Upload(file);

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    // Positive-ish: if parsing succeeds and returns rows, controller should render Results.
    // Note: scraper is new()'d inside controller; this test only verifies short-circuit behavior up to parsing.
    // To make full positive unit tests possible, consider injecting the scraper behind an interface.
    [Fact]
    public async Task Upload_WhenParseReturnsNoRows_ReturnsIndexWithError_Negative()
    {
        var excel = new Mock<ExcelProcessingService>();
        excel.Setup(e => e.ParseExcel(It.IsAny<Stream>())).Returns([]);

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger);

        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(content, 0, content.Length, "file", "a.xlsx");

        var result = await controller.Upload(file);

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Upload_WhenFileTooLarge_ReturnsIndexWithError_Negative()
    {
        var excel = new Mock<ExcelProcessingService>();
        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger);

        var size = 10 * 1024 * 1024 + 1; // > 10MB
        var content = new MemoryStream(new byte[size]);
        var file = new FormFile(content, 0, content.Length, "file", "a.xlsx");

        var result = await controller.Upload(file);

        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Index");
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        excel.Verify(e => e.ParseExcel(It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task Upload_WhenParseReturnsRows_UsesScraperAndReturnsResults_Positive()
    {
        var excel = new Mock<ExcelProcessingService>();
        excel.Setup(e => e.ParseExcel(It.IsAny<Stream>())).Returns(
        [
            new ComparisonItem { Name = "Laptop Dell 5510", Price = 1000m }
        ]);

        var scraper = new Mock<IPromScraper>();
        scraper.Setup(s => s.FindProductsAsync("Laptop Dell 5510", 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PromProduct("Laptop Dell 5510", "1200"),
                new PromProduct("Mouse", "100"),
            ]);

        var factory = new Mock<IPromScraperFactory>();
        factory.Setup(f => f.Create(true)).Returns(scraper.Object);

        var logger = new Mock<IAppLogger>();
        var controller = CreateController(excel, logger, factory);

        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(content, 0, content.Length, "file", "a.xlsx");

        var result = await controller.Upload(file);

        var view = result.Should().BeOfType<ViewResult>().Which;
        view.ViewName.Should().Be("Results");
        var model = view.Model.Should().BeAssignableTo<List<ComparisonItem>>().Which;
        model.Should().ContainSingle();
        model[0].MarketPrice.Should().NotBeNull();

        scraper.Verify(s => s.FindProductsAsync("Laptop Dell 5510", 15, It.IsAny<CancellationToken>()), Times.Once);
    }
}
