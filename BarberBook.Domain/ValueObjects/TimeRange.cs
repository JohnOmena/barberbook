using System;
using System.Collections.Generic;
using System.Linq;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Domain.ValueObjects;

public readonly record struct TimeRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeSpan Duration => End - Start;

    public TimeRange(DateTime start, DateTime end)
    {
        if (end <= start)
        {
            throw new DomainException("TimeRange inválido: End deve ser maior que Start.");
        }

        Start = start;
        End = end;
    }

    public bool Overlaps(TimeRange other)
        => Start < other.End && End > other.Start;

    public IEnumerable<TimeRange> Subtract(IEnumerable<TimeRange> others)
    {
        if (others is null) throw new DomainException("A coleção de intervalos para subtrair não pode ser nula.");

        if (!others.Any())
        {
            yield return this;
            yield break;
        }

        var self = this;
        var intersections = new List<TimeRange>();
        foreach (var o in others)
        {
            if (!self.Overlaps(o)) continue;
            var inter = Intersect(self, o);
            if (inter.HasValue)
            {
                intersections.Add(inter.Value);
            }
        }
        intersections = intersections
            .OrderBy(r => r.Start)
            .ToList();

        if (intersections.Count == 0)
        {
            yield return this;
            yield break;
        }

        var cursor = Start;
        foreach (var r in intersections)
        {
            if (r.End <= cursor)
            {
                continue; // totalmente antes do cursor atual
            }

            if (r.Start > cursor)
            {
                yield return new TimeRange(cursor, r.Start);
                cursor = r.End;
            }
            else
            {
                if (r.End > cursor)
                {
                    cursor = r.End; // estende a área subtraída mesclada
                }
            }
        }

        if (cursor < End)
        {
            yield return new TimeRange(cursor, End);
        }
    }

    private static TimeRange? Intersect(TimeRange a, TimeRange b)
    {
        var start = a.Start > b.Start ? a.Start : b.Start;
        var end = a.End < b.End ? a.End : b.End;
        return end > start ? new TimeRange(start, end) : null;
    }
}
