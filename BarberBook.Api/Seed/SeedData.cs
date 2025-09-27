using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

        // Services (8 items; update or insert). BufferMin=5, precos configurados conforme tabela base
        var desiredServices = new (string Name, int DurationMin, decimal Price)[]
        {
            ("Degrad\u00EA na Zero", 30, 25m),
            ("Degrad\u00EA Navalhado (com navalha)", 40, 30m),
            ("Corte Social (M\u00E1quina + Tesoura)", 20, 23m),
            ("Corte S\u00F3 M\u00E1quina", 20, 20m),
            ("Corte na Tesoura", 30, 35m),
            ("Barba", 20, 20m),
            ("Acabamento (Pezinho) + Sobrancelha", 10, 10m),
            ("Combo: Cabelo + Barba", 50, 45m)
        };

        var servicesBySlug = await db.Services
            .Where(s => s.TenantId == tenant.Id)
            .ToDictionaryAsync(s => s.Slug);

        foreach (var (name, duration, price) in desiredServices)
        {
            var slug = ToKebabCase(name);
            if (servicesBySlug.TryGetValue(slug, out var svc))
            {
                // Update if any difference (construct new and replace tracked entity values)
                var updated = new Service(svc.Id, tenant.Id, name, slug, duration, 5, price, true);
                db.Entry(svc).CurrentValues.SetValues(updated);
            }
            else
            {
                db.Services.Add(new Service(Guid.NewGuid(), tenant.Id, name, slug, duration, 5, price, true));
            }
        }

        await db.SaveChangesAsync();

        // Sample appointments for today (only if none exist) to facilitar testes/UX
        var today = DateTime.UtcNow.Date;
        var anyToday = await db.Appointments.AnyAsync(a => a.StartsAt >= today && a.StartsAt < today.AddDays(1));
        if (!anyToday)
        {
            var svc = await db.Services.Where(s => s.TenantId == tenant.Id && s.Active).OrderBy(s => s.Name).FirstOrDefaultAsync();
            if (svc is not null)
            {
                var times = new[] { new TimeSpan(12,0,0), new TimeSpan(13,0,0), new TimeSpan(14,0,0) };
                foreach (var ts in times)
                {
                    var sUtc = DateTime.SpecifyKind(today.Add(ts), DateTimeKind.Utc);
                    var eUtc = sUtc.AddMinutes(svc.DurationMin + svc.BufferMin);
                    db.Appointments.Add(new Appointment(
                        id: Guid.NewGuid(),
                        tenantId: tenant.Id,
                        professionalId: prof.Id,
                        serviceId: svc.Id,
                        startsAtUtc: sUtc,
                        endsAtUtc: eUtc,
                        status: BarberBook.Domain.Enums.AppointmentStatus.Confirmed,
                        clientName: $"Cliente {ts.Hours:00}:{ts.Minutes:00}",
                        clientContact: "+5511999999999",
                        createdAtUtc: DateTime.UtcNow));
                }
                await db.SaveChangesAsync();
            }
        }

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
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", "");
        cleaned = Regex.Replace(cleaned, @"[\s_]+", "-");
        cleaned = Regex.Replace(cleaned, @"-+", "-");
        return cleaned.Trim('-');
    }
}
