using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;

namespace BarberBook.UnitTests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.FromResult(1);
    }
}

