using System.Security.Cryptography;
using System.Text;
using AcademiaDigital.Domain.Interfaces.Services;
using BC = BCrypt.Net.BCrypt;

namespace AcademiaDigital.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BC.HashPassword(password);

    public bool Verify(string password, string hash)
        => hash.StartsWith("$2")
            ? BC.Verify(password, hash)
            : VerifyDjangoPassword(password, hash);

    // Compatibilidad con hashes Django PBKDF2-SHA256 existentes
    // Formato: pbkdf2_sha256$<iterations>$<salt>$<hash>
    private static bool VerifyDjangoPassword(string password, string djangoHash)
    {
        var parts = djangoHash.Split('$');
        if (parts.Length < 4 || parts[0] != "pbkdf2_sha256") return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;

        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            Encoding.UTF8.GetBytes(parts[2]),
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return Convert.ToBase64String(derivedKey) == parts[3];
    }
}
