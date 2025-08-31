using System;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Domain.Entities;

public sealed class Availability
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public Guid ProfessionalId { get; }
    public byte Weekday { get; private set; } // 1..7
    public TimeSpan Start { get; private set; }
    public TimeSpan End { get; private set; }

    private Availability() { }

    public Availability(Guid id, Guid tenantId, Guid professionalId, byte weekday, TimeSpan start, TimeSpan end)
    {
        if (id == Guid.Empty) throw new DomainException("Id da Disponibilidade não pode ser vazio.");
        if (tenantId == Guid.Empty) throw new DomainException("TenantId da Disponibilidade não pode ser vazio.");
        if (professionalId == Guid.Empty) throw new DomainException("ProfessionalId da Disponibilidade não pode ser vazio.");
        if (weekday < 1 || weekday > 7) throw new DomainException("Weekday deve estar no intervalo 1..7.");
        if (end <= start) throw new DomainException("Horário inválido: End deve ser maior que Start.");

        Id = id;
        TenantId = tenantId;
        ProfessionalId = professionalId;
        Weekday = weekday;
        Start = start;
        End = end;
    }
}
