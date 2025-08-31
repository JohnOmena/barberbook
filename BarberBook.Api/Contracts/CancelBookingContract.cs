using System;

namespace BarberBook.Api.Contracts;

public readonly record struct CancelBookingContract(
    Guid AppointmentId,
    string? Reason);

