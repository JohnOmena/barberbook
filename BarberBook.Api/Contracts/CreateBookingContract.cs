using System;

namespace BarberBook.Api.Contracts;

public readonly record struct CreateBookingContract(
    Guid TenantId,
    Guid ServiceId,
    DateTime StartUtc,
    string ClientName,
    string ClientContact);

