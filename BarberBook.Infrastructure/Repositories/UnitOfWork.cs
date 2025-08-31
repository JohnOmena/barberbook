using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BarberBook.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _db;

    public UnitOfWork(DbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}

