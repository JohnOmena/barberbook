using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Application.DTOs;
using BarberBook.Application.Validations;
using BarberBook.Domain.Entities;
using BarberBook.Domain.Enums;
using BarberBook.Domain.Exceptions;
using FluentValidation;

namespace BarberBook.Application.UseCases;

public sealed class CreateBookingUseCase
{
    private readonly IRepository<Professional> _professionals;
    private readonly IRepository<Service> _services;
    private readonly IRepository<Appointment> _appointments;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateBookingRequest> _validator;
    private readonly IDateTimeProvider _clock;

    public CreateBookingUseCase(
        IRepository<Professional> professionals,
        IRepository<Service> services,
        IRepository<Appointment> appointments,
        IUnitOfWork uow,
        IValidator<CreateBookingRequest> validator,
        IDateTimeProvider clock)
    {
        _professionals = professionals;
        _services = services;
        _appointments = appointments;
        _uow = uow;
        _validator = validator;
        _clock = clock;
    }

    public async Task<BookingResponse> HandleAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var prof = _professionals.Query().FirstOrDefault(p => p.IsDefault && p.Active);
        if (prof is null) throw new DomainException("Profissional padrão não encontrado.");

        var service = _services.Query().FirstOrDefault(s => s.Id == request.ServiceId && s.Active);
        if (service is null) throw new DomainException("Serviço não encontrado ou inativo.");

        var start = request.StartUtc;
        if (start.Kind != DateTimeKind.Utc)
            throw new DomainException("StartUtc deve estar em UTC.");

        var end = start.AddMinutes(service.DurationMin + service.BufferMin);

        var exists = _appointments.Query()
            .Any(a => a.ProfessionalId == prof.Id && a.StartsAt == start);
        if (exists)
            throw new DomainConflictException("Já existe um agendamento nesse horário.");

        var appt = new Appointment(
            id: Guid.NewGuid(),
            tenantId: request.TenantId,
            professionalId: prof.Id,
            serviceId: service.Id,
            startsAtUtc: start,
            endsAtUtc: end,
            status: AppointmentStatus.Confirmed,
            clientName: request.ClientName,
            clientContact: request.ClientContact,
            createdAtUtc: _clock.UtcNow);

        _appointments.Add(appt);
        await _uow.SaveChangesAsync(cancellationToken);

        return new BookingResponse(appt.Id, appt.StartsAt, appt.EndsAt, appt.Status);
    }
}
