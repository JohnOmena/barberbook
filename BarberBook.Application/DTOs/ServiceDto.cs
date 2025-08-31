using System;

namespace BarberBook.Application.DTOs;

public readonly record struct ServiceDto(
    Guid Id,
    string Name,
    string Slug,
    int DurationMin,
    int BufferMin,
    decimal Price,
    bool Active);

