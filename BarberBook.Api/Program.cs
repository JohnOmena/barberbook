using System.Text.Json.Serialization;
using BarberBook.Api.Extensions;
using BarberBook.Api.Endpoints;
using BarberBook.Api.Middleware;
using BarberBook.Api.Seed;
using BarberBook.Application.Abstractions;
using BarberBook.Application.Services;
using BarberBook.Application.DTOs;
using BarberBook.Application.Validations;
using BarberBook.Infrastructure;
using FluentValidation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog (console + rolling file)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Swagger/OpenAPI
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// CORS: permitir localhost e IPs privados (para acesso via celular na LAN) em Development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Localhost", p => p
        .SetIsOriginAllowed(origin =>
        {
            try
            {
                var u = new Uri(origin);
                if (u.Scheme != "http" && u.Scheme != "https") return false;
                if (string.Equals(u.Host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(u.Host, "host.docker.internal", StringComparison.OrdinalIgnoreCase)) return true;
                // permitir IPs privados típicos (10/8, 172.16-31/12, 192.168/16)
                if (System.Net.IPAddress.TryParse(u.Host, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var b = ip.GetAddressBytes();
                    var isPrivate = b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
                    if (isPrivate) return true;
                }
                // permitir tudo em Development
                return builder.Environment.IsDevelopment();
            }
            catch { return false; }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// DI: Infrastructure + Application services
builder.Services.AddInfrastructurePersistence(builder.Configuration);
builder.Services.AddScoped<ISlotCalculator, SlotCalculator>();
builder.Services.AddScoped<IValidator<CreateBookingRequest>, CreateBookingRequestValidator>();
builder.Services.AddScoped<BarberBook.Application.UseCases.GetServicesUseCase>();
builder.Services.AddScoped<BarberBook.Application.UseCases.GetSlotsUseCase>();
builder.Services.AddScoped<BarberBook.Application.UseCases.CreateBookingUseCase>();
builder.Services.AddScoped<BarberBook.Application.UseCases.CancelBookingUseCase>();
builder.Services.AddScoped<BarberBook.Application.UseCases.GetDayStatusUseCase>();
builder.Services.AddScoped<BarberBook.Application.UseCases.UpdateAppointmentStatusUseCase>();
builder.Services.AddScoped<BarberBook.Application.UseCases.DeleteAppointmentUseCase>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BarberBook API", Version = "v1" });
    c.DocumentFilter<BarberBook.Api.Swagger.OrderTagsDocumentFilter>();
    c.OrderActionsBy(apiDesc => apiDesc.RelativePath);
});
var app = builder.Build();

// Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS
app.UseCors("Localhost");

await app.MigrateAsync();
var seedEnabled = builder.Configuration.GetValue<bool>("Seed:Enabled", true);
if (seedEnabled)
{
    await SeedData.SeedAsync(app);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BarberBook API v1");
    });
}

app.UseHttpsRedirection();

// Basic auth placeholder for /admin/*
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/admin"), appBuilder =>
{
    appBuilder.UseMiddleware<BasicAuthMiddleware>();
});

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// API endpoints
app.MapServicesEndpoints();
app.MapSlotsEndpoints();
app.MapBookingEndpoints();
app.MapStatusEndpoints();
app.MapTenantsEndpoints();

app.Run();

public partial class Program { }


