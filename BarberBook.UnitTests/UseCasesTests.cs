using System;
using System.Linq;
using System.Threading.Tasks;
using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using BarberBook.Application.Services;
using BarberBook.Application.Validations;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.UnitTests.Fakes;
using FluentValidation;
using Xunit;

namespace BarberBook.UnitTests;

public class UseCasesTests
{
    [Fact]
    public async Task GetServices_ReturnsActiveOrdered()
    {
        var tenantId = Guid.NewGuid();
        var servicesRepo = new Fakes.FakeRepository<Service>(new[]
        {
            new Service(Guid.NewGuid(), tenantId, "B", "b", 30, 5, 0m, true),
            new Service(Guid.NewGuid(), tenantId, "A", "a", 30, 5, 0m, true),
            new Service(Guid.NewGuid(), tenantId, "Z", "z", 30, 5, 0m, false),
        });
        var uc = new GetServicesUseCase(servicesRepo);
        var list = await uc.HandleAsync();
        Assert.Equal(new[] { "A", "B" }, list.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task GetSlots_GeneratesFromAvailability()
    {
        var tenantId = Guid.NewGuid();
        var prof = new Professional(Guid.NewGuid(), tenantId, "Pro", isDefault: true, active: true);
        var profRepo = new Fakes.FakeRepository<Professional>(new[] { prof });
        var svc = new Service(Guid.NewGuid(), tenantId, "Corte", "corte", 30, 5, 0m, true);
        var svcRepo = new Fakes.FakeRepository<Service>(new[] { svc });
        var apptRepo = new Fakes.FakeRepository<Appointment>();
        var avail = new Availability(Guid.NewGuid(), tenantId, prof.Id, weekday: 3, // Wednesday
            start: new TimeSpan(9, 0, 0), end: new TimeSpan(10, 0, 0));
        var availRepo = new Fakes.FakeRepository<Availability>(new[] { avail });
        var calc = new SlotCalculator();
        var clock = new FixedClock(new DateTime(2025, 1, 1, 7, 0, 0, DateTimeKind.Utc));
        var uc = new GetSlotsUseCase(profRepo, svcRepo, apptRepo, availRepo, calc, clock);

        // Wednesday 2025-01-01 is actually Wednesday
        var slots = await uc.HandleAsync(svc.Id, new DateOnly(2025, 1, 1));
        Assert.NotEmpty(slots);
        Assert.Equal(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), slots.First().Start);
    }

    [Fact]
    public async Task CreateBooking_CreatesConfirmedAndComputesEnd()
    {
        var tenantId = Guid.NewGuid();
        var prof = new Professional(Guid.NewGuid(), tenantId, "Pro", isDefault: true, active: true);
        var profRepo = new Fakes.FakeRepository<Professional>(new[] { prof });
        var svc = new Service(Guid.NewGuid(), tenantId, "Corte", "corte", 30, 5, 0m, true);
        var svcRepo = new Fakes.FakeRepository<Service>(new[] { svc });
        var apptRepo = new Fakes.FakeRepository<Appointment>();
        var uow = new FakeUnitOfWork();
        var clock = new FixedClock(new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        IValidator<CreateBookingRequest> validator = new CreateBookingRequestValidator(clock);
        var uc = new CreateBookingUseCase(profRepo, svcRepo, apptRepo, uow, validator, clock);

        var start = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var req = new CreateBookingRequest(tenantId, svc.Id, start, "João", "99999-0000");
        var resp = await uc.HandleAsync(req);

        Assert.Equal(AppointmentStatus.Confirmed, resp.Status);
        Assert.Equal(start.AddMinutes(35), resp.EndsAt);
        Assert.Equal(1, uow.Saves);
    }

    [Fact]
    public async Task CancelBooking_SetsStatusCancelled()
    {
        var tenantId = Guid.NewGuid();
        var appt = new Appointment(Guid.NewGuid(), tenantId, Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 9, 30, 0, DateTimeKind.Utc),
            AppointmentStatus.Confirmed, "Cliente", "Contato", new DateTime(2024, 12, 31, 12, 0, 0, DateTimeKind.Utc));
        var apptRepo = new Fakes.FakeRepository<Appointment>(new[] { appt });
        var uow = new FakeUnitOfWork();
        var uc = new CancelBookingUseCase(apptRepo, uow);
        await uc.HandleAsync(appt.Id);
        Assert.Equal(AppointmentStatus.Cancelled, appt.Status);
    }

    [Fact]
    public async Task GetDayStatus_ReturnsTotalsAndCash()
    {
        var tenantId = Guid.NewGuid();
        var svc1 = new Service(Guid.NewGuid(), tenantId, "Corte", "corte", 30, 5, 50m, true);
        var svc2 = new Service(Guid.NewGuid(), tenantId, "Barba", "barba", 30, 5, 30m, true);
        var services = new Fakes.FakeRepository<Service>(new[] { svc1, svc2 });
        var a1 = new Appointment(Guid.NewGuid(), tenantId, Guid.NewGuid(), svc1.Id,
            new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 9, 30, 0, DateTimeKind.Utc),
            AppointmentStatus.Done, "C1", "X", new DateTime(2024, 12, 31, 12, 0, 0, DateTimeKind.Utc));
        var a2 = new Appointment(Guid.NewGuid(), tenantId, Guid.NewGuid(), svc2.Id,
            new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 10, 30, 0, DateTimeKind.Utc),
            AppointmentStatus.Confirmed, "C2", "Y", new DateTime(2024, 12, 31, 12, 0, 0, DateTimeKind.Utc));
        var appts = new Fakes.FakeRepository<Appointment>(new[] { a1, a2 });

        var uc = new GetDayStatusUseCase(appts, services);
        var resp = await uc.HandleAsync(new DateOnly(2025, 1, 1));
        Assert.Equal(2, resp.Totals);
        Assert.Equal(50m, resp.Cash);
        Assert.Equal("Corte", resp.Items.First().ServiceName);
    }
}

