using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BarberBook.Application.DTOs;

public sealed record UpcomingResponse(
    [property: JsonPropertyName("appointments")] IReadOnlyList<UpcomingItemDto> Appointments);

