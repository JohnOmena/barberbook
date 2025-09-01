using BarberBook.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BarberBook.Api.Endpoints;

public static class TenantsEndpoints
{
    public static IEndpointRouteBuilder MapTenantsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenant-default", async Task<Results<Ok<TenantDto>, NotFound>> (AppDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.OrderBy(t => t.Name).FirstOrDefaultAsync(ct);
            if (tenant is null) return TypedResults.NotFound();
            return TypedResults.Ok(new TenantDto(tenant.Id, tenant.Name));
        })
        .WithName("GetDefaultTenant")
        .WithTags("Status")
        .WithOpenApi(op =>
        {
            op.Summary = "Obtém o tenant padrão";
            op.Description = "Retorna o primeiro tenant (por nome). Em dev, o seed cria 'Default'.";
            return op;
        });

        return app;
    }
}

public readonly record struct TenantDto(Guid Id, string Name);
