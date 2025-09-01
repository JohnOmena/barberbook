using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

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
        .WithTags("Services")
        .WithOpenApi(op =>
        {
            op.Summary = "Lista serviços ativos";
            op.Description = "Retorna os serviços ativos ordenados por nome.";
            op.Responses["200"].Description = "Lista de serviços";
            return op;
        })
        .WithOpenApi(op =>
        {
            var arr = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["id"] = new OpenApiString("22222222-2222-2222-2222-222222222222"),
                    ["name"] = new OpenApiString("Corte Masculino"),
                    ["slug"] = new OpenApiString("corte-masculino"),
                    ["durationMin"] = new OpenApiInteger(30),
                    ["bufferMin"] = new OpenApiInteger(5),
                    ["price"] = new OpenApiDouble(50.0),
                    ["active"] = new OpenApiBoolean(true)
                }
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
