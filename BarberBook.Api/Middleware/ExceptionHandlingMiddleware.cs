using System.Net;
using System.Text.Json;
using BarberBook.Domain.Exceptions;
using FluentValidation;

namespace BarberBook.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var payload = new
            {
                error = "validation",
                message = "Validation failed",
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (DomainConflictException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var payload = new { error = "conflict", message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var payload = new { error = "domain", message = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
