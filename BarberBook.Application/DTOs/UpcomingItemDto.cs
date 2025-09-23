using System;

namespace BarberBook.Application.DTOs;

public readonly record struct UpcomingItemDto(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string ServiceName);
