using System;
using System.Collections.Generic;
using System.Linq;
using BarberBook.Application.Abstractions;
using BarberBook.Domain.ValueObjects;

namespace BarberBook.Application.Services;

public sealed class SlotCalculator : ISlotCalculator
{
    // As per spec: lead time in minutes
    private const int leadMinutes = 30;

    public IEnumerable<TimeRange> BuildSlots(
        TimeRange openHours,
        IEnumerable<TimeRange> busy,
        int durationMin,
        int bufferMin,
        int stepMin,
        DateTime utcNow,
        TimeZoneInfo displayTz)
    {
        if (durationMin <= 0) yield break;
        if (bufferMin < 0) yield break;
        if (stepMin <= 0) yield break;

        var minStart = Max(openHours.Start, utcNow.AddMinutes(leadMinutes));
        var busyList = (busy ?? Enumerable.Empty<TimeRange>()).ToList();

        foreach (var free in openHours.Subtract(busyList))
        {
            var segmentStart = Max(free.Start, minStart);
            var cursor = RoundUpToStep(segmentStart, stepMin);
            var slotLength = TimeSpan.FromMinutes(durationMin + bufferMin);

            while (true)
            {
                var slotEnd = cursor + slotLength;
                if (slotEnd > free.End) break; // não atravessar openHours.End/free.End

                yield return new TimeRange(cursor, cursor.AddMinutes(durationMin)); // retorno do slot apenas na duração do serviço

                var next = cursor.AddMinutes(stepMin);
                if (next >= free.End) break;
                cursor = next;
            }
        }
    }

    private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;

    private static DateTime RoundUpToStep(DateTime dt, int stepMin)
    {
        // rounds up to the nearest stepMin boundary (00, 15, 30, 45) preserving Kind
        var kind = dt.Kind;
        dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
        var remainder = dt.Minute % stepMin;
        if (remainder != 0)
        {
            dt = dt.AddMinutes(stepMin - remainder);
        }
        return DateTime.SpecifyKind(dt, kind);
    }
}

