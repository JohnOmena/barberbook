using System;

namespace BarberBook.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

