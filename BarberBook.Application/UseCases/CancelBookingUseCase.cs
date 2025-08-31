using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Application.UseCases;

public sealed class CancelBookingUseCase
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IUnitOfWork _uow;

    public CancelBookingUseCase(IRepository<Appointment> appointments, IUnitOfWork uow)
    {
        _appointments = appointments;
        _uow = uow;
    }

    public async Task HandleAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appt = _appointments.Query().FirstOrDefault(a => a.Id == appointmentId);
        if (appt is null) throw new DomainException("Agendamento não encontrado.");

        // Set status
        // Status has private setter; domain constructor sets value; here we need to mutate.
        // Using reflection to set private setter without changing domain invariants.
        typeof(Appointment).GetProperty(nameof(Appointment.Status))!.SetValue(appt, AppointmentStatus.Cancelled);

        _appointments.Update(appt);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}

