using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Application.UseCases;

public sealed class UpdateAppointmentUseCase
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Service> _services;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public UpdateAppointmentUseCase(
        IRepository<Appointment> appointments,
        IRepository<Service> services,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _appointments = appointments;
        _services = services;
        _uow = uow;
        _clock = clock;
    }

    public async Task HandleAsync(Guid appointmentId, Guid serviceId, DateTimeOffset start, string clientName, string clientContact, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var appt = _appointments.Query().FirstOrDefault(a => a.Id == appointmentId);
        if (appt is null) throw new DomainException("Agendamento nao encontrado.");

        var startUtc = start.ToUniversalTime();

        var service = _services.Query().FirstOrDefault(s => s.Id == serviceId && s.Active);
        if (service is null) throw new DomainException("Servico nao encontrado ou inativo.");

        if (startUtc <= _clock.UtcNow)
            throw new DomainException("StartUtc deve ser no futuro.");

        var endUtc = startUtc.AddMinutes(service.DurationMin + service.BufferMin);

        // Conflitos: ignorar o proprio agendamento
        var conflict = _appointments.Query().Any(a =>
            a.Id != appt.Id &&
            a.ProfessionalId == appt.ProfessionalId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.NoShow &&
            a.StartsAt < endUtc &&
            a.EndsAt > startUtc);
        if (conflict)
            throw new DomainConflictException("Ja existe um agendamento que conflita com esse horario.");

        // Atualiza campos (propriedades com setters privados)
        typeof(Appointment).GetProperty(nameof(Appointment.ServiceId))!.SetValue(appt, service.Id);
        typeof(Appointment).GetProperty(nameof(Appointment.StartsAt))!.SetValue(appt, startUtc);
        typeof(Appointment).GetProperty(nameof(Appointment.EndsAt))!.SetValue(appt, endUtc);
        typeof(Appointment).GetProperty(nameof(Appointment.ClientName))!.SetValue(appt, (clientName ?? string.Empty).Trim());
        typeof(Appointment).GetProperty(nameof(Appointment.ClientContact))!.SetValue(appt, (clientContact ?? string.Empty).Trim());
        typeof(Appointment).GetProperty(nameof(Appointment.UpdatedAt))!.SetValue(appt, _clock.UtcNow);
        if (!string.IsNullOrWhiteSpace(updatedBy))
            typeof(Appointment).GetProperty(nameof(Appointment.UpdatedBy))!.SetValue(appt, updatedBy);

        _appointments.Update(appt);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
