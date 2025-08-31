using System;

namespace BarberBook.Application.DTOs;

public readonly record struct SlotDto(
    DateTime StartUtc,
    DateTime EndUtc);

