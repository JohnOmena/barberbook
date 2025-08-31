using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;

namespace BarberBook.Api.Endpoints;

public static class ServicesEndpoints
{
    public static IEndpointRouteBuilder MapServicesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/services", async Task<Ok<IReadOnlyList<ServiceDto>>> (GetServicesUseCase uc, CancellationToken ct) =>
        {
            var list = await uc.HandleAsync(ct);
            return TypedResults.Ok(list);
        })
        .WithName("GetServices")
        .WithOpenApi(op =>
        {
            op.Summary = "Lista serviços ativos";
            op.Description = "Retorna os serviços ativos ordenados por nome.";
            op.Responses["200"].Description = "Lista de serviços";
            return op;
        });

        return app;
    }
}
