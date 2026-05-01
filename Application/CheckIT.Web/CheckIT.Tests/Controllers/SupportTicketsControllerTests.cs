using CheckIT.Tests.Integration;
using CheckIT.Web.Data;
using CheckIT.Web.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CheckIT.Tests.Controllers;

public class SupportTicketsControllerTests
{
    [Fact]
    public async Task My_WhenAuthenticated_Returns200()
    {
        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "u1", roles: ["User"]);
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/SupportTickets/My");

        resp.IsSuccessStatusCode.Should().BeTrue();
        (await resp.Content.ReadAsStringAsync()).Should().Contain("Мої звернення");
    }

    [Fact]
    public async Task Create_Post_WhenValid_PersistsTicketAndRedirects()
    {
        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "u1", roles: ["User"]);
        var client = factory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["Subject"] = "Need unblock",
            ["Message"] = "Please review my account block reason.",
        };

        var resp = await client.PostAsync("/SupportTickets/Create", new FormUrlEncodedContent(form));

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/SupportTickets/My");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SupportTickets.Should().HaveCount(1);

        var t = db.SupportTickets.Single();
        t.Subject.Should().Be("Need unblock");
        t.Status.Should().Be(SupportTicketStatus.Open);
        t.UserId.Should().Be("u1");
    }

    [Fact]
    public async Task Create_Post_WhenInvalid_Returns200_AndDoesNotPersist()
    {
        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "u1", roles: ["User"]);
        var client = factory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["Subject"] = "a",
            ["Message"] = "b",
        };

        var resp = await client.PostAsync("/SupportTickets/Create", new FormUrlEncodedContent(form));

        resp.IsSuccessStatusCode.Should().BeTrue();
        (await resp.Content.ReadAsStringAsync()).Should().Contain("text-danger");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SupportTickets.Should().BeEmpty();
    }

    [Fact]
    public async Task Details_WhenNotOwnerAndNotAdmin_Returns403()
    {
        var ticketId = Guid.NewGuid();

        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "u1", roles: ["User"]);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupportTickets.Add(new SupportTicket
            {
                Id = ticketId,
                UserId = "u2",
                Subject = "Subject ok",
                Message = "Long enough message for ticket.",
                Status = SupportTicketStatus.Open,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/SupportTickets/Details/{ticketId}");

        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanViewSupportTicket_FromOtherUser()
    {
        var ticketId = Guid.NewGuid();

        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "admin", roles: ["Admin"]);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupportTickets.Add(new SupportTicket
            {
                Id = ticketId,
                UserId = "u2",
                Subject = "Subject ok",
                Message = "Long enough message for ticket.",
                Status = SupportTicketStatus.Open,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/SupportTickets/Details/{ticketId}");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_UpdateTicket_ChangesStatusAndComment()
    {
        var ticketId = Guid.NewGuid();

        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "admin", roles: ["Admin"]);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SupportTickets.Add(new SupportTicket
            {
                Id = ticketId,
                UserId = "u2",
                Subject = "Subject ok",
                Message = "Long enough message for ticket.",
                Status = SupportTicketStatus.Open,
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var form = new Dictionary<string, string>
        {
            ["Id"] = ticketId.ToString(),
            ["Status"] = ((int)SupportTicketStatus.Resolved).ToString(),
            ["AdminComment"] = "Reviewed. Unblocked.",
        };

        var resp = await client.PostAsync("/Admin/SupportTicket", new FormUrlEncodedContent(form));
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updated = db.SupportTickets.Single(t => t.Id == ticketId);
            updated.Status.Should().Be(SupportTicketStatus.Resolved);
            updated.AdminComment.Should().Be("Reviewed. Unblocked.");
        }
    }

    [Fact]
    public async Task Admin_SupportTicketsPage_Returns200()
    {
        await using var factory = new CustomWebApplicationFactoryWithRole(userId: "admin", roles: ["Admin"]);
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/Admin/SupportTickets");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }
}
