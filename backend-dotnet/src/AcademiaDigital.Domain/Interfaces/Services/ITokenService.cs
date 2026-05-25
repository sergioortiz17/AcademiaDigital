using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    long? GetUserIdFromToken(string token);
    UserRole? GetUserRoleFromToken(string token);
}
