using System;

namespace BarberBook.Application.DTOs;

public readonly record struct CreateBookingRequest(
    Guid TenantId,
    Guid ServiceId,
    DateTime StartUtc,
    string ClientName,
    string ClientContact);

