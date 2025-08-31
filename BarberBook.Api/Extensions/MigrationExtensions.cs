using System.Threading.Tasks;
using BarberBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberBook.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task MigrateAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

