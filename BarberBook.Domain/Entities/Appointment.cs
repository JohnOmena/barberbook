using System;
using BarberBook.Domain.Enums;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Domain.Entities;

public sealed class Appointment
{
    public Guid Id { get; }
    public Guid TenantId { get; }
    public Guid ProfessionalId { get; }
    public Guid ServiceId { get; }
    public DateTime StartsAt { get; private set; } // UTC
    public DateTime EndsAt { get; private set; }   // UTC
    public AppointmentStatus Status { get; private set; }
    public string ClientName { get; private set; } = default!;
    public string ClientContact { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } // UTC
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    private Appointment() { }

    public Appointment(
        Guid id,
        Guid tenantId,
        Guid professionalId,
        Guid serviceId,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        AppointmentStatus status,
        string clientName,
        string clientContact,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty) throw new DomainException("Id do Agendamento não pode ser vazio.");
        if (tenantId == Guid.Empty) throw new DomainException("TenantId do Agendamento não pode ser vazio.");
        if (professionalId == Guid.Empty) throw new DomainException("ProfessionalId do Agendamento não pode ser vazio.");
        if (serviceId == Guid.Empty) throw new DomainException("ServiceId do Agendamento não pode ser vazio.");
        if (string.IsNullOrWhiteSpace(clientName)) throw new DomainException("ClientName é obrigatório.");
        if (string.IsNullOrWhiteSpace(clientContact)) throw new DomainException("ClientContact é obrigatório.");

        if (startsAtUtc.Kind != DateTimeKind.Utc || endsAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("StartsAt e EndsAt devem estar em UTC.");
        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("CreatedAt deve estar em UTC.");
        if (endsAtUtc <= startsAtUtc)
            throw new DomainException("EndsAt deve ser maior que StartsAt.");

        Id = id;
        TenantId = tenantId;
        ProfessionalId = professionalId;
        ServiceId = serviceId;
        StartsAt = startsAtUtc;
        EndsAt = endsAtUtc;
        Status = status;
        ClientName = clientName.Trim();
        ClientContact = clientContact.Trim();
        CreatedAt = createdAtUtc;
    }
}
