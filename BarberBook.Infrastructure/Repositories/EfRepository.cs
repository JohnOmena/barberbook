using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BarberBook.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly DbContext _db;

    public EfRepository(DbContext db)
    {
        _db = db;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Set<T>().FindAsync(new object?[] { id }, cancellationToken).AsTask();

    public void Add(T entity) => _db.Set<T>().Add(entity);
    public void Update(T entity) => _db.Set<T>().Update(entity);
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
    public IQueryable<T> Query() => _db.Set<T>().AsQueryable();
}
