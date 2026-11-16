using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Control;

/// <summary>
/// Catálogo de la plataforma (instituciones y dominios), en el esquema "control" de la base
/// compartida. Las tablas académicas de cada institución están en su propio esquema.
/// </summary>
public class ControlDbContext(DbContextOptions<ControlDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();
    public DbSet<PlatformRequest> PlatformRequests => Set<PlatformRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("Tenants");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Slug).HasMaxLength(30).IsRequired();
            builder.HasIndex(t => t.Slug).IsUnique();
            builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
            builder.Property(t => t.SchemaName).HasMaxLength(63).IsRequired();
            builder.HasIndex(t => t.SchemaName).IsUnique();
            builder.Property(t => t.IsActive).HasDefaultValue(true);
            builder.Property(t => t.TeacherEmailDomain).HasMaxLength(253);
        });

        modelBuilder.Entity<PlatformRequest>(builder =>
        {
            builder.ToTable("PlatformRequests");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.InstitutionName).HasMaxLength(200).IsRequired();
            builder.Property(r => r.RequestedSlug).HasMaxLength(30);
            builder.Property(r => r.ContactName).HasMaxLength(200).IsRequired();
            builder.Property(r => r.ContactEmail).HasMaxLength(254).IsRequired();
            builder.Property(r => r.ContactPhone).HasMaxLength(40);
            builder.Property(r => r.Location).HasMaxLength(200);
            builder.Property(r => r.Message).HasMaxLength(2000);
            builder.Property(r => r.TenantSlug).HasMaxLength(30);
            builder.HasIndex(r => new { r.Status, r.CreatedAt });
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
