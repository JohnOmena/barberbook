using System;
using BarberBook.Domain.Enums;

namespace BarberBook.Application.DTOs;

public readonly record struct BookingResponse(
    Guid Id,
    DateTime StartsAt,
    DateTime EndsAt,
    AppointmentStatus Status);

