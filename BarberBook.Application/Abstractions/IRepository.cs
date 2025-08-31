using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BarberBook.Application.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
    IQueryable<T> Query();
}

