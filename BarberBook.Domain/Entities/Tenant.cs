using System;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; }
    public string Name { get; private set; } = default!;

    private Tenant() { }

    public Tenant(Guid id, string name)
    {
        if (id == Guid.Empty) throw new DomainException("Id do Tenant não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Nome do Tenant é obrigatório.");

        Id = id;
        Name = name.Trim();
    }
}
