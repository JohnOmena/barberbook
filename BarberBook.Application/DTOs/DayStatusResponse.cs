using System;
using System.Collections.Generic;
using BarberBook.Domain.Enums;

namespace BarberBook.Application.DTOs;

public readonly record struct DayStatusItemDto(
    Guid Id,
    DateTime StartsAt,
    DateTime EndsAt,
    string ServiceName,
    string ClientName,
    string ClientContact,
    AppointmentStatus Status,
    decimal Price);

public sealed record DayStatusResponse(
    IReadOnlyList<DayStatusItemDto> Items,
    int Totals,
    decimal Cash);
