using System.Globalization;
using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;

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
        .WithOpenApi(op =>
        {
            op.Summary = "Status do dia";
            op.Description = "Retorna cartões do painel, totais e caixa do dia.";
            return op;
        });

        return app;
    }
}
