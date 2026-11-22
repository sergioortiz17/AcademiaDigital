using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class ProfileContactRepository(AppDbContext db) : IProfileContactRepository
{
    public async Task<ProfileContactInfo?> GetAsync(long userId, CancellationToken ct = default)
    {
        var student = await db.Students.AsNoTracking().Where(s => s.UserId == userId)
            .Select(s => new ProfileContactInfo(s.AddressLine, s.City, s.Province, s.PostalCode,
                s.EmergencyContactName, s.EmergencyContactRelationship, s.EmergencyContactPhone, s.HealthInsurance))
            .FirstOrDefaultAsync(ct);
        if (student is not null) return student;
        return await db.Teachers.AsNoTracking().Where(t => t.UserId == userId)
            .Select(t => new ProfileContactInfo(t.AddressLine, t.City, t.Province, t.PostalCode,
                t.EmergencyContactName, t.EmergencyContactRelationship, t.EmergencyContactPhone, t.HealthInsurance))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> SaveAsync(long userId, ProfileContactInfo info, CancellationToken ct = default)
    {
        // Entidad rastreada sin navegaciones: SaveChanges actualiza solo las columnas modificadas.
        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (student is not null)
        {
            student.AddressLine = info.AddressLine;
            student.City = info.City;
            student.Province = info.Province;
            student.PostalCode = info.PostalCode;
            student.EmergencyContactName = info.EmergencyContactName;
            student.EmergencyContactRelationship = info.EmergencyContactRelationship;
            student.EmergencyContactPhone = info.EmergencyContactPhone;
            student.HealthInsurance = info.HealthInsurance;
            student.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }

        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacher is null) return false;
        teacher.AddressLine = info.AddressLine;
        teacher.City = info.City;
        teacher.Province = info.Province;
        teacher.PostalCode = info.PostalCode;
        teacher.EmergencyContactName = info.EmergencyContactName;
        teacher.EmergencyContactRelationship = info.EmergencyContactRelationship;
        teacher.EmergencyContactPhone = info.EmergencyContactPhone;
        teacher.HealthInsurance = info.HealthInsurance;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
