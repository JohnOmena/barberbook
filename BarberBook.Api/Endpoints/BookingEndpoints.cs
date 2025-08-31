using BarberBook.Api.Contracts;
using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;

namespace BarberBook.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/book", async Task<Results<Ok<BookingResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> (CreateBookingContract contract,
            CreateBookingUseCase uc,
            IValidator<CreateBookingRequest> validator,
            CancellationToken ct) =>
        {
            // Validate using FluentValidation
            var request = new CreateBookingRequest(contract.TenantId, contract.ServiceId, contract.StartUtc, contract.ClientName, contract.ClientContact);
            await validator.ValidateAndThrowAsync(request, ct);

            var result = await uc.HandleAsync(request, ct);
            return TypedResults.Ok(result);
        })
        .WithName("CreateBooking")
        .WithOpenApi(op =>
        {
            op.Summary = "Cria um agendamento";
            op.Description = "Cria um novo agendamento Confirmed calculando o término (duração+buffer).";
            return op;
        });

        app.MapPost("/api/cancel", async Task<NoContent> (CancelBookingContract contract, CancelBookingUseCase uc, CancellationToken ct) =>
        {
            await uc.HandleAsync(contract.AppointmentId, ct);
            return TypedResults.NoContent();
        })
        .WithName("CancelBooking");

        app.MapPost("/api/appointments/{id:guid}/status/{status}", async Task<Results<NoContent, BadRequest<ProblemDetails>>> (HttpContext http, Guid id, string status, UpdateAppointmentStatusUseCase uc, CancellationToken ct) =>
        {
            if (!Enum.TryParse<BarberBook.Domain.Enums.AppointmentStatus>(status, ignoreCase: true, out var st))
                return TypedResults.BadRequest(new ProblemDetails { Title = "Parâmetro inválido", Detail = "status inválido" });
            var updatedBy = http.Request.Headers.TryGetValue("X-User", out var hv) ? hv.ToString() : null;
            await uc.HandleAsync(id, st, updatedBy, ct);
            return TypedResults.NoContent();
        })
        .WithName("UpdateAppointmentStatus")
        .WithOpenApi(op =>
        {
            op.Summary = "Atualiza status do agendamento";
            op.Description = "Transições válidas: Confirmed/Pending→CheckIn, CheckIn→InService, InService→Done, Confirmed→NoShow (após 15min).";
            return op;
        });

        return app;
    }
}
