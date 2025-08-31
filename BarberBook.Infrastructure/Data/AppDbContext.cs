using System;
using System.Reflection;
using BarberBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberBook.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public bool EnableTenantFilter { get; set; } = false;
    public Guid TenantId { get; set; }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter prepared for future multi-tenant enablement
        modelBuilder.Entity<Professional>().HasQueryFilter(e => !EnableTenantFilter || e.TenantId == TenantId);
        modelBuilder.Entity<Service>().HasQueryFilter(e => !EnableTenantFilter || e.TenantId == TenantId);
        modelBuilder.Entity<Availability>().HasQueryFilter(e => !EnableTenantFilter || e.TenantId == TenantId);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !EnableTenantFilter || e.TenantId == TenantId);
    }
}

