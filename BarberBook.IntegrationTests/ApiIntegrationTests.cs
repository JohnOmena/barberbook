using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BarberBook.Api;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace BarberBook.IntegrationTests;

public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithDatabase("barberbook_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        var conn = _pg.GetConnectionString();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    var overrides = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = conn,
                        ["Seed:Enabled"] = "false",
                        ["Logging:LogLevel:Default"] = "Warning"
                    };
                    config.AddInMemoryCollection(overrides!);
                });
            });

        // Ensure database is migrated
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Seed minimal data for tests (no automatic seed)
        var tenant = new Tenant(Guid.NewGuid(), "Test");
        db.Tenants.Add(tenant);
        var prof = new Professional(Guid.NewGuid(), tenant.Id, "Pro", isDefault: true, active: true);
        db.Professionals.Add(prof);
        var svc = new Service(Guid.NewGuid(), tenant.Id, "Corte", "corte", 30, 5, 0m, true);
        db.Services.Add(svc);
        // Availabilities Mon-Sun 09:00-18:00 for tests
        for (byte d = 1; d <= 7; d++)
        {
            db.Availabilities.Add(new Availability(Guid.NewGuid(), tenant.Id, prof.Id, d, new TimeSpan(9,0,0), new TimeSpan(18,0,0)));
        }
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            _factory.Dispose();
        }
        await _pg.StopAsync();
        await _pg.DisposeAsync();
    }

    [Fact]
    public async Task GetSlots_ReturnsSlots()
    {
        Assert.NotNull(_factory);
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var svc = db.Services.First();

        var client = _factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var res = await client.GetAsync($"/api/slots?serviceId={svc.Id}&date={today:yyyy-MM-dd}");
        res.EnsureSuccessStatusCode();
        var slots = await res.Content.ReadFromJsonAsync<List<SlotDto>>();
        Assert.NotNull(slots);
        Assert.NotEmpty(slots!);
    }

    [Fact]
    public async Task Book_Cancel_And_Conflict()
    {
        Assert.NotNull(_factory);
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var svc = db.Services.First();
        var tenantId = db.Tenants.Select(t => t.Id).First();

        var client = _factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var slots = await client.GetFromJsonAsync<List<SlotDto>>($"/api/slots?serviceId={svc.Id}&date={today:yyyy-MM-dd}") ?? new();
        Assert.NotEmpty(slots);
        var slot = slots.First();

        var createReq = new CreateBookingRequest(tenantId, svc.Id, slot.StartUtc, "Teste", "999");
        var bookRes = await client.PostAsJsonAsync("/api/book", createReq);
        bookRes.EnsureSuccessStatusCode();
        var booking = await bookRes.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(AppointmentStatus.Pending, booking.Status);

        // Cancel
        var cancelRes = await client.PostAsJsonAsync("/api/cancel", new { appointmentId = booking.Id, reason = "test" });
        Assert.Equal(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // Conflict (try booking same time again)
        var conflictRes = await client.PostAsJsonAsync("/api/book", createReq);
        Assert.Equal(HttpStatusCode.Conflict, conflictRes.StatusCode);
    }
}

public readonly record struct SlotDto(DateTime StartUtc, DateTime EndUtc);
public readonly record struct CreateBookingRequest(Guid TenantId, Guid ServiceId, DateTime StartUtc, string ClientName, string ClientContact);
public readonly record struct BookingResponse(Guid Id, DateTime StartsAt, DateTime EndsAt, AppointmentStatus Status);

