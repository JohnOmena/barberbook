using BarberBook.Api.Contracts;
using BarberBook.Application.DTOs;
using BarberBook.Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;

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
        .WithTags("Bookings")
        .WithOpenApi(op =>
        {
            op.Summary = "Cria um agendamento";
            op.Description = "Cria um novo agendamento Confirmed calculando o término (duração+buffer).";
            return op;
        })
        .WithOpenApi(op =>
        {
            op.RequestBody ??= new OpenApiRequestBody();
            if (!op.RequestBody.Content.TryGetValue("application/json", out var mt))
            {
                mt = new OpenApiMediaType();
                op.RequestBody.Content["application/json"] = mt;
            }
            mt.Example = new OpenApiObject
            {
                ["tenantId"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                ["serviceId"] = new OpenApiString("22222222-2222-2222-2222-222222222222"),
                ["startUtc"] = new OpenApiString("2025-08-31T12:00:00Z"),
                ["clientName"] = new OpenApiString("Fulano"),
                ["clientContact"] = new OpenApiString("+5511999999999")
            };
            return op;
        })
        .WithOpenApi(op =>
        {
            if (op.Responses.ContainsKey("200") && op.Responses["200"].Content.ContainsKey("application/json"))
            {
                op.Responses["200"].Content["application/json"].Example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                    ["startsAt"] = new OpenApiString("2025-08-31T12:00:00Z"),
                    ["endsAt"] = new OpenApiString("2025-08-31T12:35:00Z"),
                    ["status"] = new OpenApiString("Confirmed")
                };
            }
            return op;
        })
        .WithOpenApi(op =>
        {
            if (op.Responses.ContainsKey("400") && op.Responses["400"].Content.ContainsKey("application/json"))
            {
                op.Responses["400"].Content["application/json"].Example = new OpenApiObject
                {
                    ["title"] = new OpenApiString("Parâmetro inválido"),
                    ["detail"] = new OpenApiString("status inválido")
                };
            }
            return op;
        });

        app.MapPost("/api/cancel", async Task<NoContent> (CancelBookingContract contract, CancelBookingUseCase uc, CancellationToken ct) =>
        {
            await uc.HandleAsync(contract.AppointmentId, ct);
            return TypedResults.NoContent();
        })
        .WithName("CancelBooking")
        .WithTags("Bookings")
        .WithOpenApi(op =>
        {
            op.Summary = "Cancela um agendamento";
            op.Description = "Cancela o agendamento informado.";
            return op;
        })
        .WithOpenApi(op =>
        {
            op.RequestBody ??= new OpenApiRequestBody();
            if (!op.RequestBody.Content.TryGetValue("application/json", out var mt))
            {
                mt = new OpenApiMediaType();
                op.RequestBody.Content["application/json"] = mt;
            }
            mt.Example = new OpenApiObject
            {
                ["appointmentId"] = new OpenApiString("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                ["reason"] = new OpenApiString("Cliente solicitou")
            };
            return op;
        });

        app.MapPost("/api/appointments/{id:guid}/status/{status}", async Task<Results<NoContent, BadRequest<ProblemDetails>>> (HttpContext http, Guid id, string status, UpdateAppointmentStatusUseCase uc, CancellationToken ct) =>
        {
            if (!Enum.TryParse<BarberBook.Domain.Enums.AppointmentStatus>(status, ignoreCase: true, out var st))
                return TypedResults.BadRequest(new ProblemDetails { Title = "Parâmetro inválido", Detail = "status inválido" });
            var updatedBy = http.Request.Headers.TryGetValue("X-User", out var hv) ? hv.ToString() : null;
            await uc.HandleAsync(id, st, updatedBy, ct);
            return TypedResults.NoContent();
        })
        .WithName("UpdateAppointmentStatus")
        .WithTags("Bookings")
        .WithOpenApi(op =>
        {
            op.Summary = "Atualiza status do agendamento";
            op.Description = "Transições válidas: Confirmed/Pending→CheckIn, CheckIn→InService, InService→Done, Confirmed→NoShow (após 15min).";
            return op;
        });

        app.MapDelete("/api/appointments/{id:guid}", async Task<NoContent> (Guid id, DeleteAppointmentUseCase uc, CancellationToken ct) =>
        {
            await uc.HandleAsync(id, ct);
            return TypedResults.NoContent();
        })
        .WithName("DeleteAppointment")
        .WithTags("Bookings")
        .WithOpenApi(op =>
        {
            op.Summary = "Exclui um agendamento";
            op.Description = "Remove definitivamente o agendamento informado.";
            return op;
        });

        return app;
    }
}
