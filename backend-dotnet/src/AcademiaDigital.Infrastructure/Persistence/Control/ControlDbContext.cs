using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Control;

/// <summary>
/// Base de control de la plataforma: catálogo de instituciones y sus dominios. Es independiente
/// de las bases académicas (una por institución, ver <see cref="AppDbContext"/>).
/// </summary>
public class ControlDbContext(DbContextOptions<ControlDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("Tenants");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Slug).HasMaxLength(30).IsRequired();
            builder.HasIndex(t => t.Slug).IsUnique();
            builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
            builder.Property(t => t.DatabaseName).HasMaxLength(63).IsRequired();
            builder.HasIndex(t => t.DatabaseName).IsUnique();
            builder.Property(t => t.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<TenantDomain>(builder =>
        {
            builder.ToTable("TenantDomains");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Host).HasMaxLength(253).IsRequired();
            // Un host pertenece a una sola institución: es la clave de resolución.
            builder.HasIndex(d => d.Host).IsUnique();
            builder.HasOne(d => d.Tenant).WithMany(t => t.Domains)
                .HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>Fábrica de diseño para <c>dotnet ef --context ControlDbContext</c>.</summary>
public sealed class ControlDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ControlDbContext>
{
    public ControlDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DOTNET_EF_CONTROL_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=AcademiaDigital_control;Username=postgres;Password=postgres;";
        return new ControlDbContext(new DbContextOptionsBuilder<ControlDbContext>().UseNpgsql(connectionString).Options);
    }
}
