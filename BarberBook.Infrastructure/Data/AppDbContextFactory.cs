using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BarberBook.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private readonly string? _connectionString;

    public AppDbContextFactory()
    {
    }

    public AppDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var conn = _connectionString
                   ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                   ?? "Host=localhost;Port=5432;Database=barberbook;Username=postgres;Password=postgres";

        optionsBuilder
            .UseNpgsql(conn);

        return new AppDbContext(optionsBuilder.Options);
    }
}
