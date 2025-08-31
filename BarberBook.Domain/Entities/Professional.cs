using System;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Domain.Entities;

public sealed class Professional
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public string Name { get; private set; } = default!;
    public bool IsDefault { get; private set; } = true;
    public bool Active { get; private set; }

    private Professional() { }

    public Professional(Guid id, Guid tenantId, string name, bool isDefault = true, bool active = true)
    {
        if (id == Guid.Empty) throw new DomainException("Id do Profissional não pode ser vazio.");
        if (tenantId == Guid.Empty) throw new DomainException("TenantId do Profissional não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Nome do Profissional é obrigatório.");

        Id = id;
        TenantId = tenantId;
        Name = name.Trim();
        IsDefault = isDefault;
        Active = active;
    }
}
