using System;
using BarberBook.Application.Abstractions;
using BarberBook.Application.DTOs;
using FluentValidation;

namespace BarberBook.Application.Validations;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.ServiceId)
            .NotEmpty();

        RuleFor(x => x.ClientName)
            .NotEmpty()
            .MinimumLength(2);

        RuleFor(x => x.ClientContact)
            .NotEmpty()
            .MinimumLength(5);

        RuleFor(x => x.StartUtc)
            .Must(dt => dt.Kind == DateTimeKind.Utc)
            .WithMessage("StartUtc deve estar em UTC.")
            .Must(dt => dt > clock.UtcNow)
            .WithMessage("StartUtc deve ser no futuro.");
    }
}

