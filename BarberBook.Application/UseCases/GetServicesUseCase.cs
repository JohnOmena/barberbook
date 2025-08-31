using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using BarberBook.Application.DTOs;
using BarberBook.Domain.Entities;

namespace BarberBook.Application.UseCases;

public sealed class GetServicesUseCase
{
    private readonly IRepository<Service> _services;

    public GetServicesUseCase(IRepository<Service> services)
    {
        _services = services;
    }

    public Task<IReadOnlyList<ServiceDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var result = _services.Query()
            .Where(s => s.Active)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto(s.Id, s.Name, s.Slug, s.DurationMin, s.BufferMin, s.Price, s.Active))
            .ToList()
            .AsReadOnly();

        return Task.FromResult((IReadOnlyList<ServiceDto>)result);
    }
}

