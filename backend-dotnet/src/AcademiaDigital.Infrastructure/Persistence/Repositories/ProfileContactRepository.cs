using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class ProfileContactRepository(AppDbContext db) : IProfileContactRepository
{
    public Task<ProfileContactInfo?> GetAsync(long userId, CancellationToken ct = default)
        => db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new ProfileContactInfo(u.DocumentType, u.AlternatePhone, u.AddressLine, u.Neighborhood,
                u.City, u.District, u.Province, u.Country, u.PostalCode, u.EmergencyContactName,
                u.EmergencyContactRelationship, u.EmergencyContactPhone, u.HealthInsurance))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> SaveAsync(long userId, ProfileContactInfo info, CancellationToken ct = default)
    {
        // Entidad rastreada: SaveChanges actualiza solo las columnas modificadas.
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return false;
        user.DocumentType = info.DocumentType;
        user.AlternatePhone = info.AlternatePhone;
        user.AddressLine = info.AddressLine;
        user.Neighborhood = info.Neighborhood;
        user.City = info.City;
        user.District = info.District;
        user.Province = info.Province;
        user.Country = info.Country;
        user.PostalCode = info.PostalCode;
        user.EmergencyContactName = info.EmergencyContactName;
        user.EmergencyContactRelationship = info.EmergencyContactRelationship;
        user.EmergencyContactPhone = info.EmergencyContactPhone;
        user.HealthInsurance = info.HealthInsurance;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
