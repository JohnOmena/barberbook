using System.Globalization;
using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace BarberBook.Api.Endpoints;

public static class SlotsEndpoints
{
    public static IEndpointRouteBuilder MapSlotsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/slots", async Task<Results<BadRequest<ProblemDetails>, Ok<IEnumerable<SlotDto>>>> (Guid serviceId, string date, GetSlotsUseCase uc, CancellationToken ct) =>
        {
            if (serviceId == Guid.Empty) return TypedResults.BadRequest(new ProblemDetails { Title = "Parâmetro inválido", Detail = "serviceId inválido." });
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return TypedResults.BadRequest(new ProblemDetails { Title = "Parâmetro inválido", Detail = "date inválido. Formato esperado: YYYY-MM-DD" });

            var slots = await uc.HandleAsync(serviceId, d, ct);
            var dto = slots.Select(s => new SlotDto(s.Start, s.End));
            return TypedResults.Ok(dto);
        })
        .WithName("GetSlots")
        .WithTags("Slots")
        .WithOpenApi(op =>
        {
            op.Summary = "Consulta de slots";
            op.Description = "Retorna os horários disponíveis para o serviço na data informada.";
            op.Parameters[0].Description = "Id do serviço";
            op.Parameters[1].Description = "Data no formato YYYY-MM-DD";
            return op;
        })
        .WithOpenApi(op =>
        {
            var arr = new OpenApiArray
            {
                new OpenApiObject { ["startUtc"] = new OpenApiString("2025-08-31T12:00:00Z"), ["endUtc"] = new OpenApiString("2025-08-31T12:30:00Z") },
                new OpenApiObject { ["startUtc"] = new OpenApiString("2025-08-31T12:35:00Z"), ["endUtc"] = new OpenApiString("2025-08-31T13:05:00Z") }
            };
            if (op.Responses.ContainsKey("200") && op.Responses["200"].Content.ContainsKey("application/json"))
            {
                op.Responses["200"].Content["application/json"].Example = arr;
            }
            return op;
        });

        return app;
    }
}
