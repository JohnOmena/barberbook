using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Application.DTOs;
using BarberBook.Domain.Entities;

namespace BarberBook.Application.UseCases;

public sealed class GetUpcomingAppointmentsUseCase
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Service> _services;

    public GetUpcomingAppointmentsUseCase(IRepository<Appointment> appointments, IRepository<Service> services)
    {
        _appointments = appointments;
        _services = services;
    }

    public Task<IReadOnlyList<UpcomingItemDto>> HandleAsync(int daysAhead = 14, CancellationToken cancellationToken = default)
    {
        if (daysAhead < 0) daysAhead = 0;
        // Same time zone logic used in GetDayStatusUseCase
        var tz = ResolveBusinessTimeZone();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var localStart = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        // include today plus 'daysAhead' full days (end exclusive)
        var endUtc = startUtc.AddDays(daysAhead + 1);

        var services = _services.Query().ToDictionary(s => s.Id);
        var appts = _appointments.Query()
            .Where(a => a.StartsAt >= startUtc && a.StartsAt < endUtc)
            .OrderBy(a => a.StartsAt)
            .ToList();

        var items = appts.Select(a =>
        {
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(a.StartsAt, tz);
            var endLocal = TimeZoneInfo.ConvertTimeFromUtc(a.EndsAt, tz);
            var startDto = new DateTimeOffset(startLocal, tz.GetUtcOffset(a.StartsAt));
            var endDto = new DateTimeOffset(endLocal, tz.GetUtcOffset(a.EndsAt));
            return new UpcomingItemDto(
                startDto,
                endDto,
                services.TryGetValue(a.ServiceId, out var svc) ? svc.Name : string.Empty
            );
        }).ToList();

        return Task.FromResult((IReadOnlyList<UpcomingItemDto>)items);
    }

    private static System.TimeZoneInfo ResolveBusinessTimeZone()
    {
        var envTz = System.Environment.GetEnvironmentVariable("BB_TIMEZONE")
                 ?? System.Environment.GetEnvironmentVariable("TZ")
                 ?? System.Environment.GetEnvironmentVariable("TIMEZONE");
        string[] ids = new[]
        {
            envTz,
            "America/Sao_Paulo",
            "America/Maceio",
            "E. South America Standard Time",
            "SA Eastern Standard Time"
        };
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            try { return System.TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }
        }
        // Fallback: fixed -03:00 (BRT)
        return System.TimeZoneInfo.CreateCustomTimeZone("BRT", System.TimeSpan.FromHours(-3), "BRT", "BRT");
    }
}
