using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.Domain.ValueObjects;

namespace BarberBook.Application.UseCases;

public sealed class GetSlotsUseCase
{
    private readonly IRepository<Professional> _professionals;
    private readonly IRepository<Service> _services;
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Availability> _availabilities;
    private readonly ISlotCalculator _slotCalculator;
    private readonly IDateTimeProvider _clock;

    public GetSlotsUseCase(
        IRepository<Professional> professionals,
        IRepository<Service> services,
        IRepository<Appointment> appointments,
        IRepository<Availability> availabilities,
        ISlotCalculator slotCalculator,
        IDateTimeProvider clock)
    {
        _professionals = professionals;
        _services = services;
        _appointments = appointments;
        _availabilities = availabilities;
        _slotCalculator = slotCalculator;
        _clock = clock;
    }

    public Task<IReadOnlyList<TimeRange>> HandleAsync(Guid serviceId, DateOnly date, CancellationToken cancellationToken = default)
    {
        // Default professional
        var prof = _professionals.Query().FirstOrDefault(p => p.IsDefault && p.Active);
        if (prof is null)
            return Task.FromResult((IReadOnlyList<TimeRange>)Array.Empty<TimeRange>());

        var service = _services.Query().FirstOrDefault(s => s.Id == serviceId && s.Active);
        if (service is null)
            return Task.FromResult((IReadOnlyList<TimeRange>)Array.Empty<TimeRange>());

        // Availabilities for weekday (1..7). If multiple segments exist, compute slots per segment and merge.
        var weekday = (byte)((int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek); // Sunday=0 -> 7
        var avails = _availabilities.Query().Where(a => a.TenantId == prof.TenantId && a.ProfessionalId == prof.Id && a.Weekday == weekday).ToList();

        // If no availability registered, return empty
        if (avails.Count == 0)
            return Task.FromResult((IReadOnlyList<TimeRange>)Array.Empty<TimeRange>());

        var utcDayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);

        var busy = _appointments.Query()
            .Where(a => a.ProfessionalId == prof.Id && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .Select(a => new TimeRange(a.StartsAt, a.EndsAt))
            .ToList();

        var result = new List<TimeRange>();
        foreach (var a in avails)
        {
            var open = new TimeRange(utcDayStart + a.Start, utcDayStart + a.End);
            var slots = _slotCalculator.BuildSlots(open, busy, service.DurationMin, service.BufferMin, stepMin: 15, _clock.UtcNow, TimeZoneInfo.Utc);
            result.AddRange(slots);
        }

        return Task.FromResult((IReadOnlyList<TimeRange>)result);
    }
}
