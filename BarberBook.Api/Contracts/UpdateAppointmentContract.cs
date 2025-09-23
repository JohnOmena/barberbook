using System;
using System.Text.Json.Serialization;

namespace BarberBook.Api.Contracts;

public readonly record struct UpdateAppointmentContract(
    Guid ServiceId,
    [property: JsonPropertyName("startUtc")] DateTimeOffset Start,
    string ClientName,
    string ClientContact);
