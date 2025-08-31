using System;
using BarberBook.Application.Abstractions;

namespace BarberBook.UnitTests.Fakes;

public sealed class FixedClock : IDateTimeProvider
{
    public FixedClock(DateTime utcNow) { UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc); }
    public DateTime UtcNow { get; }
}

