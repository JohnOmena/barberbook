using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BarberBook.Api.Swagger;

public sealed class OrderTagsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new() { Name = "Bookings", Description = "Operações de agendamentos" },
            new() { Name = "Services", Description = "Serviços disponíveis" },
            new() { Name = "Slots", Description = "Horários disponíveis" },
            new() { Name = "Status", Description = "Resumo do dia" }
        };
    }
}

