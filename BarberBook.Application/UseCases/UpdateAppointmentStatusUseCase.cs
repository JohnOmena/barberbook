using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Application.UseCases;

public sealed class UpdateAppointmentStatusUseCase
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public UpdateAppointmentStatusUseCase(IRepository<Appointment> appointments, IUnitOfWork uow, IDateTimeProvider clock)
    {
        _appointments = appointments;
        _uow = uow;
        _clock = clock;
    }

    public async Task HandleAsync(Guid appointmentId, AppointmentStatus status, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var appt = _appointments.Query().FirstOrDefault(a => a.Id == appointmentId);
        if (appt is null) throw new DomainException("Agendamento não encontrado.");

        var current = appt.Status;
        var now = _clock.UtcNow;

        bool allowed = status switch
        {
            AppointmentStatus.CheckIn => current is AppointmentStatus.Confirmed or AppointmentStatus.Pending,
            AppointmentStatus.InService => current is AppointmentStatus.CheckIn,
            AppointmentStatus.Done => current is AppointmentStatus.InService,
            AppointmentStatus.NoShow => current is AppointmentStatus.Confirmed && now >= appt.StartsAt.AddMinutes(15),
            AppointmentStatus.Cancelled => true,
            AppointmentStatus.Confirmed or AppointmentStatus.Pending => true,
            _ => false
        };

        if (!allowed)
            throw new DomainException($"Transição inválida: {current} -> {status}.");

        typeof(Appointment).GetProperty(nameof(Appointment.Status))!.SetValue(appt, status);
        typeof(Appointment).GetProperty(nameof(Appointment.UpdatedAt))!.SetValue(appt, now);
        if (updatedBy is not null)
            typeof(Appointment).GetProperty(nameof(Appointment.UpdatedBy))!.SetValue(appt, updatedBy);
        _appointments.Update(appt);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
