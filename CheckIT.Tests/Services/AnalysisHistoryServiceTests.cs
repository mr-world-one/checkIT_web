using CheckIT.Web.Data;
using CheckIT.Web.Models;
using CheckIT.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CheckIT.Tests.Services;

public class AnalysisHistoryServiceTests
{
	private static AppDbContext CreateInMemoryDb()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new AppDbContext(options);
	}

	private static List<ComparisonItem> SampleItems() =>
	[
		new ComparisonItem { Name = "Dell", Price = 25000m, MarketPrice = 22000m },
		new ComparisonItem { Name = "Logitech", Price = 500m, MarketPrice = 450m }
	];

	[Fact]
	public async Task SaveAsync_SavesEntryToDatabase_Positive()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);

		await service.SaveAsync("user1", "Excel", "tender.xlsx", SampleItems());

		var entries = await db.AnalysisHistories.ToListAsync();
		entries.Should().HaveCount(1);
		entries[0].UserId.Should().Be("user1");
		entries[0].Type.Should().Be("Excel");
		entries[0].Source.Should().Be("tender.xlsx");
		entries[0].ItemsCount.Should().Be(2);
	}

	[Fact]
	public async Task SaveAsync_SerializesItemsToJson_Positive()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);

		await service.SaveAsync("user1", "Prozorro", "UA-2024-001", SampleItems());

		var entry = await db.AnalysisHistories.FirstAsync();
		entry.ItemsJson.Should().NotBeNullOrEmpty();
		var deserialized = service.DeserializeItems(entry.ItemsJson);
		deserialized.Should().HaveCount(2);
		deserialized[0].Name.Should().Be("Dell");
	}

	[Fact]
	public async Task GetUserHistoryAsync_ReturnsOnlyUserEntries_Positive()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);

		await service.SaveAsync("user1", "Excel", "file1.xlsx", SampleItems());
		await service.SaveAsync("user2", "Excel", "file2.xlsx", SampleItems());
		await service.SaveAsync("user1", "Prozorro", "UA-001", SampleItems());

		var result = await service.GetUserHistoryAsync("user1");

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(h => h.UserId.Should().Be("user1"));
	}

	[Fact]
	public async Task GetUserHistoryAsync_ReturnsEntriesOrderedByDateDescending_Positive()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);

		await service.SaveAsync("user1", "Excel", "file1.xlsx", SampleItems());
		await Task.Delay(10);
		await service.SaveAsync("user1", "Excel", "file2.xlsx", SampleItems());

		var result = await service.GetUserHistoryAsync("user1");

		result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
	}

	[Fact]
	public async Task GetUserHistoryAsync_WhenNoEntries_ReturnsEmptyList_Negative()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);

		var result = await service.GetUserHistoryAsync("user1");

		result.Should().BeEmpty();
	}

	[Fact]
	public void DeserializeItems_ReturnsCorrectItems_Positive()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);
		var items = SampleItems();
		var json = System.Text.Json.JsonSerializer.Serialize(items);

		var result = service.DeserializeItems(json);

		result.Should().HaveCount(2);
		result[0].Name.Should().Be("Dell");
		result[0].Price.Should().Be(25000m);
		result[0].MarketPrice.Should().Be(22000m);
	}

	[Fact]
	public void DeserializeItems_WhenEmptyJson_ReturnsEmptyList_Negative()
	{
		using var db = CreateInMemoryDb();
		var service = new AnalysisHistoryService(db);

		var result = service.DeserializeItems("[]");

		result.Should().BeEmpty();
	}
}
