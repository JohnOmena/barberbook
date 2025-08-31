using System;
using System.Collections.Generic;
using BarberBook.Domain.ValueObjects;

namespace BarberBook.Application.Abstractions;

public interface ISlotCalculator
{
    IEnumerable<TimeRange> BuildSlots(
        TimeRange openHours,
        IEnumerable<TimeRange> busy,
        int durationMin,
        int bufferMin,
        int stepMin,
        DateTime utcNow,
        TimeZoneInfo displayTz);
}

