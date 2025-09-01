using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Exceptions;

namespace BarberBook.Application.UseCases;

public sealed class DeleteAppointmentUseCase
{
    private readonly IRepository<Appointment> _appointments;
    private readonly IUnitOfWork _uow;

    public DeleteAppointmentUseCase(IRepository<Appointment> appointments, IUnitOfWork uow)
    {
        _appointments = appointments;
        _uow = uow;
    }

    public async Task HandleAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appt = await _appointments.GetByIdAsync(appointmentId, cancellationToken);
        if (appt is null)
            throw new DomainException("Agendamento no encontrado.");

        _appointments.Remove(appt);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}

