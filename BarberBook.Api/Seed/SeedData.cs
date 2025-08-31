using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BarberBook.Domain.Entities;
using BarberBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberBook.Api.Seed;

public static class SeedData
{
    public static async Task SeedAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Tenant default
        var tenantName = "Default";
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == tenantName);
        if (tenant is null)
        {
            tenant = new Tenant(Guid.NewGuid(), tenantName);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        // Professional default
        var prof = await db.Professionals
            .FirstOrDefaultAsync(p => p.TenantId == tenant.Id && p.IsDefault);
        if (prof is null)
        {
            prof = new Professional(Guid.NewGuid(), tenant.Id, name: "Profissional", isDefault: true, active: true);
            db.Professionals.Add(prof);
            await db.SaveChangesAsync();
        }
        else
        {
            // ensure active/default flags
            // (no setters per domain, but constructor set implies invariant; if properties were mutable we'd update)
        }

        // Services (8 items; update or insert). BufferMin=5, Price=0, Active=true
        var desiredServices = new (string Name, int DurationMin)[]
        {
            ("Corte de Cabelo", 30),
            ("Barba", 30),
            ("Cabelo e Barba", 60),
            ("Sobrancelha", 15),
            ("Pigmentação", 30),
            ("Hidratação", 30),
            ("Corte Infantil", 30),
            ("Corte Máquina", 20)
        };

        var servicesBySlug = await db.Services
            .Where(s => s.TenantId == tenant.Id)
            .ToDictionaryAsync(s => s.Slug);

        foreach (var (name, duration) in desiredServices)
        {
            var slug = ToKebabCase(name);
            if (servicesBySlug.TryGetValue(slug, out var svc))
            {
                // Update if any difference (construct new and replace tracked entity values)
                var updated = new Service(svc.Id, tenant.Id, name, slug, duration, 5, 0m, true);
                db.Entry(svc).CurrentValues.SetValues(updated);
            }
            else
            {
                db.Services.Add(new Service(Guid.NewGuid(), tenant.Id, name, slug, duration, 5, 0m, true));
            }
        }

        await db.SaveChangesAsync();

        // Availabilities Mon-Sat (1..6) 09:00–18:00
        var start = new TimeSpan(9, 0, 0);
        var end = new TimeSpan(18, 0, 0);

        var existingAvail = await db.Availabilities
            .Where(a => a.TenantId == tenant.Id && a.ProfessionalId == prof.Id)
            .ToListAsync();

        var desiredDays = Enumerable.Range(1, 6).Select(i => (byte)i).ToHashSet();
        var existingDays = existingAvail.Select(a => a.Weekday).ToHashSet();

        // Add missing days
        foreach (var day in desiredDays.Except(existingDays))
        {
            db.Availabilities.Add(new Availability(Guid.NewGuid(), tenant.Id, prof.Id, day, start, end));
        }

        // Update existing days if times differ
        foreach (var a in existingAvail)
        {
            if (a.Start != start || a.End != end)
            {
                var updated = new Availability(a.Id, a.TenantId, a.ProfessionalId, a.Weekday, start, end);
                db.Entry(a).CurrentValues.SetValues(updated);
            }
        }

        await db.SaveChangesAsync();
    }

    private static string ToKebabCase(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        lower = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
        lower = Regex.Replace(lower, @"[\s_]+", "-");
        lower = Regex.Replace(lower, @"-+", "-");
        return lower.Trim('-');
    }
}
