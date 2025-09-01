using System.Globalization;
using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace BarberBook.Api.Endpoints;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/status-dia", async Task<Results<BadRequest<ProblemDetails>, Ok<DayStatusResponse>>> (string date, GetDayStatusUseCase uc, CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return TypedResults.BadRequest(new ProblemDetails { Title = "Parâmetro inválido", Detail = "date inválido. Formato esperado: YYYY-MM-DD" });

            var resp = await uc.HandleAsync(d, ct);
            return TypedResults.Ok(resp);
        })
        .WithName("GetDayStatus")
        .WithTags("Status")
        .WithOpenApi(op =>
        {
            op.Summary = "Status do dia";
            op.Description = "Retorna cartões do painel, totais e caixa do dia.";
            return op;
        })
        .WithOpenApi(op =>
        {
            var items = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["id"] = new OpenApiString("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                    ["startsAt"] = new OpenApiString("2025-08-31T12:00:00Z"),
                    ["serviceName"] = new OpenApiString("Corte Masculino"),
                    ["clientName"] = new OpenApiString("Fulano"),
                    ["status"] = new OpenApiString("Confirmed"),
                    ["price"] = new OpenApiDouble(50.0)
                }
            };
            var obj = new OpenApiObject
            {
                ["items"] = items,
                ["totals"] = new OpenApiInteger(5),
                ["cash"] = new OpenApiDouble(250.0)
            };
            if (op.Responses.ContainsKey("200") && op.Responses["200"].Content.ContainsKey("application/json"))
            {
                op.Responses["200"].Content["application/json"].Example = obj;
            }
            return op;
        });

        return app;
    }
}
