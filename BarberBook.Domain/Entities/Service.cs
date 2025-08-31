using System;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Domain.Entities;

public sealed class Service
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public int DurationMin { get; private set; }
    public int BufferMin { get; private set; }
    public decimal Price { get; private set; }
    public bool Active { get; private set; }

    private Service() { }

    public Service(
        Guid id,
        Guid tenantId,
        string name,
        string slug,
        int durationMin,
        int bufferMin,
        decimal price,
        bool active = true)
    {
        if (id == Guid.Empty) throw new DomainException("Id do Serviço não pode ser vazio.");
        if (tenantId == Guid.Empty) throw new DomainException("TenantId do Serviço não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Nome do Serviço é obrigatório.");
        if (string.IsNullOrWhiteSpace(slug)) throw new DomainException("Slug do Serviço é obrigatório.");
        if (durationMin <= 0) throw new DomainException("DurationMin deve ser maior que 0.");
        if (bufferMin < 0) throw new DomainException("BufferMin deve ser maior ou igual a 0.");

        // decimal(10,2) – validar escala de 2 casas
        if (decimal.Round(price, 2) != price)
            throw new DomainException("Price deve possuir no máximo 2 casas decimais.");

        Id = id;
        TenantId = tenantId;
        Name = name.Trim();
        Slug = slug.Trim();
        DurationMin = durationMin;
        BufferMin = bufferMin;
        Price = price;
        Active = active;
    }
}
