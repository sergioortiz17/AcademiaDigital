using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    long? GetUserIdFromToken(string token);
}
