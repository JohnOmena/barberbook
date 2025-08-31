using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Application.DTOs;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;

namespace BarberBook.Application.UseCases;

public sealed class GetDayStatusUseCase
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Service> _services;

    public GetDayStatusUseCase(IRepository<Appointment> appointments, IRepository<Service> services)
    {
        _appointments = appointments;
        _services = services;
    }

    public Task<DayStatusResponse> HandleAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var appts = _appointments.Query()
            .Where(a => a.StartsAt >= dayStart && a.StartsAt < dayEnd)
            .ToList();

        var services = _services.Query().ToDictionary(s => s.Id);

        var items = new List<DayStatusItemDto>(appts.Count);
        decimal cash = 0m;
        foreach (var a in appts.OrderBy(a => a.StartsAt))
        {
            var svcName = services.TryGetValue(a.ServiceId, out var svc) ? svc.Name : "";
            var price = services.TryGetValue(a.ServiceId, out svc) ? svc.Price : 0m;
            var item = new DayStatusItemDto(a.Id, a.StartsAt, svcName, a.ClientName, a.Status, price);
            items.Add(item);
            if (a.Status == AppointmentStatus.Done)
                cash += price;
        }

        var response = new DayStatusResponse(items, items.Count, cash);
        return Task.FromResult(response);
    }
}

