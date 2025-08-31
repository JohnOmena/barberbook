using System;
using System.Linq;
using BarberBook.Application.Services;
using BarberBook.Domain.ValueObjects;
using Xunit;

namespace BarberBook.UnitTests;

public class SlotCalculatorTests
{
    [Fact]
    public void BuildSlots_NoBusy_ReturnsSlotsWithinOpenHours()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(10)); // 09:00-10:00 UTC
        var slots = calc.BuildSlots(
            open,
            Array.Empty<TimeRange>(),
            durationMin: 30,
            bufferMin: 0,
            stepMin: 15,
            utcNow: day.AddHours(8),
            displayTz: TimeZoneInfo.Utc).ToList();

        Assert.Equal(3, slots.Count); // 09:00, 09:15, 09:30 (each 30m)
        Assert.Equal(day.AddHours(9), slots[0].Start);
        Assert.Equal(day.AddHours(9).AddMinutes(30), slots[0].End);
    }

    [Fact]
    public void BuildSlots_LeadTime_RoundsUpToStep()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(10)); // 09:00-10:00 UTC
        var slots = calc.BuildSlots(
            open,
            Array.Empty<TimeRange>(),
            durationMin: 30,
            bufferMin: 0,
            stepMin: 15,
            utcNow: day.AddHours(8).AddMinutes(50), // lead 30 => earliest 09:20 -> rounded to 09:30
            displayTz: TimeZoneInfo.Utc).ToList();

        Assert.Single(slots);
        Assert.Equal(day.AddHours(9).AddMinutes(30), slots[0].Start);
    }

    [Fact]
    public void BuildSlots_WithBusy_SubtractsBusyFromOpenHours()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(10)); // 09:00-10:00
        var busy = new[] { new TimeRange(day.AddHours(9).AddMinutes(15), day.AddHours(9).AddMinutes(30)) };

        var slots = calc.BuildSlots(
            open,
            busy,
            durationMin: 30,
            bufferMin: 0,
            stepMin: 15,
            utcNow: day.AddHours(8),
            displayTz: TimeZoneInfo.Utc).ToList();

        // Expected: 09:00 and 09:30 only (09:15 blocked)
        Assert.Equal(2, slots.Count);
        Assert.Equal(day.AddHours(9), slots[0].Start);
        Assert.Equal(day.AddHours(9).AddMinutes(30), slots[1].Start);
    }

    [Fact]
    public void BuildSlots_RespectsBuffer_WhenNotFitting_DropsLateSlots()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(10)); // 60 minutes window
        // duration=30, buffer=15 => slot length=45; step=15 => starts: 09:00 (ends 09:45), 09:15 (ends 10:00) valid; 09:30 (ends 10:15) invalid
        var slots = calc.BuildSlots(open, Array.Empty<TimeRange>(), 30, 15, 15, day.AddHours(7), TimeZoneInfo.Utc).ToList();
        Assert.Equal(2, slots.Count);
        Assert.Equal(day.AddHours(9), slots[0].Start);
        Assert.Equal(day.AddHours(9).AddMinutes(30), slots[0].End);
        Assert.Equal(day.AddHours(9).AddMinutes(15), slots[1].Start);
    }

    [Fact]
    public void BuildSlots_Step10_GeneratesExpectedStarts()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(9).AddMinutes(40)); // 40 minutes
        var slots = calc.BuildSlots(open, Array.Empty<TimeRange>(), 20, 0, 10, day.AddHours(7), TimeZoneInfo.Utc).ToList();
        // Starts at 09:00 (ends 09:20), 09:10 (ends 09:30), 09:20 (ends 09:40); 09:30 would end 09:50>End
        var mins = slots.Select(s => (int)(s.Start - day.AddHours(9)).TotalMinutes).ToArray();
        Assert.Equal(new[] { 0, 10, 20 }, mins);
    }

    [Fact]
    public void BuildSlots_NoCrossOpenEnd()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(9).AddMinutes(35)); // 35 minutes
        var slots = calc.BuildSlots(open, Array.Empty<TimeRange>(), 30, 0, 5, day.AddHours(7), TimeZoneInfo.Utc).ToList();
        // 09:00 (ends 09:30) fits; 09:05 (ends 09:35) fits; 09:10 (ends 09:40) does not
        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public void BuildSlots_LeadTimeNearClose_YieldsNone()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(9).AddMinutes(40));
        // lead=30, utcNow at 09:15 => earliest 09:45 rounded to 09:50; open ends 09:40 -> none
        var slots = calc.BuildSlots(open, Array.Empty<TimeRange>(), 20, 0, 5, day.AddHours(9).AddMinutes(15), TimeZoneInfo.Utc).ToList();
        Assert.Empty(slots);
    }

    [Fact]
    public void BuildSlots_BusyOverlapsMergeProperly()
    {
        var calc = new SlotCalculator();
        var day = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new TimeRange(day.AddHours(9), day.AddHours(11)); // 2 hours
        var busy = new[]
        {
            new TimeRange(day.AddHours(9).AddMinutes(10), day.AddHours(9).AddMinutes(30)),
            new TimeRange(day.AddHours(9).AddMinutes(25), day.AddHours(9).AddMinutes(50)) // overlaps and extends
        };
        var slots = calc.BuildSlots(open, busy, 20, 0, 10, day.AddHours(7), TimeZoneInfo.Utc).ToList();
        // Busy merged covers 09:10-09:50, free windows: 09:00-09:10 and 09:50-11:00
        Assert.Contains(slots, s => s.Start == day.AddHours(9) && s.End == day.AddHours(9).AddMinutes(20));
        Assert.Contains(slots, s => s.Start == day.AddHours(9).AddMinutes(50));
    }
}
