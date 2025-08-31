using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BarberBook.Application.Abstractions;

namespace BarberBook.UnitTests.Fakes;

public sealed class FakeRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items;

    public FakeRepository(IEnumerable<T>? seed = null)
    {
        _items = seed?.ToList() ?? new List<T>();
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var prop = typeof(T).GetProperty("Id");
        var match = _items.FirstOrDefault(x => prop != null && prop.PropertyType == typeof(Guid) && (Guid)prop.GetValue(x)! == id);
        return Task.FromResult(match);
    }

    public void Add(T entity) => _items.Add(entity);
    public void Update(T entity)
    {
        // No tracking; nothing to do as we mutate original references
    }
    public void Remove(T entity) => _items.Remove(entity);
    public IQueryable<T> Query() => _items.AsQueryable();
}

