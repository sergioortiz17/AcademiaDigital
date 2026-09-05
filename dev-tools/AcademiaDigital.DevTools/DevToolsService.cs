using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.DevTools;

/// <summary>
/// Lógica de reset+reseed y listados, reutilizando el AppDbContext real.
///
/// El reset replica —a nivel de DATOS— la misma secuencia probada de
/// scripts/reset_and_reseed.sh, pero sin salir del contenedor ni destruir el
/// volumen de Docker:
///   1) EnsureDeletedAsync()  → dropea todas las tablas (equivalente a "down -v").
///   2) MigrateAsync()        → recrea el esquema aplicando la migración InitialCreate
///                              (equivalente a lo que hace el backend al arrancar).
///   3) Ejecuta, EN ORDEN, los 3 seeds .sql (DS2023 → Enfermería → usuarios demo),
///      igual que el script. El seed de usuarios demo depende de que DS2023 ya exista.
/// </summary>
public sealed class DevToolsService(AppDbContext db, IConfiguration config, ILogger<DevToolsService> logger)
{
    // Mismo orden que scripts/reset_and_reseed.sh. usuarios_demo va último (depende de DS2023).
    private static readonly string[] SeedFilesInOrder =
    [
        "seed_desarrollo_software_2023.sql",
        "seed_enfermeria.sql",
        "seed_usuarios_demo.sql"
    ];

    public async Task<ResetReport> ResetAndReseedAsync(CancellationToken ct = default)
    {
        var steps = new List<string>();

        logger.LogWarning("DevTools: RESET solicitado sobre {Target}",
            DevToolsSafety.DescribeTarget(db.Database.GetConnectionString()));

        // 1) Drop total (equivalente a down -v a nivel de datos)
        await db.Database.EnsureDeletedAsync(ct);
        steps.Add("Base eliminada (EnsureDeleted).");

        // 2) Recrear esquema aplicando migraciones (lo que hace el backend al arrancar)
        await db.Database.MigrateAsync(ct);
        steps.Add("Esquema recreado (Migrate).");

        // 3) Reseed en orden
        var seedsPath = ResolveSeedsPath();
        foreach (var file in SeedFilesInOrder)
        {
            var full = Path.Combine(seedsPath, file);
            if (!File.Exists(full))
                throw new FileNotFoundException($"No se encontró el seed '{file}' en '{seedsPath}'.", full);

            var sql = await File.ReadAllTextAsync(full, ct);
            await db.Database.ExecuteSqlRawAsync(sql, ct);
            steps.Add($"Seed aplicado: {file}");
            logger.LogInformation("DevTools: seed aplicado {File}", file);
        }

        return new ResetReport(true, DevToolsSafety.DescribeTarget(db.Database.GetConnectionString()), steps);
    }

    /// <summary>
    /// Resuelve la carpeta de seeds. Configurable por DevTools:SeedsPath (default "seeds").
    /// En docker se monta el /seeds del repo dentro del contenedor.
    /// </summary>
    private string ResolveSeedsPath()
    {
        var configured = config["DevTools:SeedsPath"] ?? "seeds";
        if (Path.IsPathRooted(configured) && Directory.Exists(configured))
            return configured;

        // buscar hacia arriba desde el working dir (dev local) y en /app (contenedor)
        foreach (var baseDir in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory, "/" })
        {
            var candidate = Path.GetFullPath(Path.Combine(baseDir, configured));
            if (Directory.Exists(candidate)) return candidate;

            // subir hasta 6 niveles buscando la carpeta seeds/
            var dir = new DirectoryInfo(baseDir);
            for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            {
                var c = Path.Combine(dir.FullName, configured);
                if (Directory.Exists(c)) return c;
            }
        }

        throw new DirectoryNotFoundException(
            $"No se encontró la carpeta de seeds ('{configured}'). " +
            "Configurá DevTools:SeedsPath o montá /seeds en el contenedor.");
    }

    // ── Listados (solo lectura) ────────────────────────────────────────────────

    public async Task<object> GetOverviewAsync(CancellationToken ct = default)
    {
        var careers = await db.Careers
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, c.IsActive })
            .ToListAsync(ct);

        var usersByRole = await db.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var users = await db.Users
            .OrderBy(u => u.Role).ThenBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.Username, u.LastName, Role = u.Role.ToString(), u.IsActive })
            .Take(200)
            .ToListAsync(ct);

        var assignments = await db.TeacherAssignments
            .Where(a => a.IsCurrent)
            .Select(a => new
            {
                a.Id,
                Teacher = a.Teacher.User.Username + " " + a.Teacher.User.LastName,
                a.TeachingPosition.CourseId,
                CourseCode = a.TeachingPosition.Course.Code,
                CourseName = a.TeachingPosition.Course.Name,
                a.TeachingPosition.AcademicYear,
                a.TeachingPosition.Semester,
                PositionType = a.TeachingPosition.PositionType.ToString(),
                a.StartedOn
            })
            .ToListAsync(ct);

        return new
        {
            target = DevToolsSafety.DescribeTarget(db.Database.GetConnectionString()),
            careers,
            usersByRole = usersByRole.Select(x => new { role = x.Role.ToString(), count = x.Count }),
            users,
            assignments
        };
    }

    public Task<List<CareerLite>> GetCareersAsync(CancellationToken ct = default) =>
        db.Careers.OrderBy(c => c.Code)
            .Select(c => new CareerLite(c.Id, c.Code, c.Name))
            .ToListAsync(ct);

    public Task<List<CourseLite>> GetCoursesByCareerAsync(int careerId, CancellationToken ct = default) =>
        db.Courses.Where(c => c.CareerId == careerId).OrderBy(c => c.Code)
            .Select(c => new CourseLite(c.Id, c.Code, c.Name))
            .ToListAsync(ct);

    public async Task<List<TeacherLite>> GetTeachersAsync(CancellationToken ct = default) =>
        await db.Teachers.Where(t => t.IsActive)
            .Select(t => new TeacherLite(t.Id, t.User.Username + " " + t.User.LastName, t.User.Email))
            .ToListAsync(ct);
}

public sealed record ResetReport(bool Success, string Target, IReadOnlyList<string> Steps);
public sealed record CareerLite(int Id, string Code, string Name);
public sealed record CourseLite(int Id, string Code, string Name);
public sealed record TeacherLite(long Id, string Name, string Email);
