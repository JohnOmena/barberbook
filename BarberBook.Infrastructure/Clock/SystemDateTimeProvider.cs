using System;
using BarberBook.Application.Abstractions;

namespace BarberBook.Infrastructure.Clock;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

