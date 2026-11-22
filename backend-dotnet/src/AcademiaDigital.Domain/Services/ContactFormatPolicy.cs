namespace AcademiaDigital.Domain.Services;

/// <summary>Formato de CUIL y teléfono argentinos para los datos del perfil.</summary>
public static class ContactFormatPolicy
{
    /// <summary>CUIL de 11 dígitos; se guarda como XX-XXXXXXXX-X. Vacío = sin CUIL.</summary>
    public static string? NormalizeCuil(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Any(c => !char.IsAsciiDigit(c) && c is not '-' and not ' ' and not '.'))
            throw new ArgumentException("El CUIL solo admite números (formato XX-XXXXXXXX-X).");
        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length != 11)
            throw new ArgumentException("El CUIL debe tener 11 dígitos (formato XX-XXXXXXXX-X).");
        return $"{digits[..2]}-{digits[2..10]}-{digits[10]}";
    }

    /// <summary>
    /// Teléfono argentino sin 0 ni 15: código de área de 2 a 4 dígitos y número de 6 a 8, que
    /// juntos suman 10 dígitos (por ejemplo 351 + 5551234).
    /// </summary>
    public static (string? AreaCode, string? Number) NormalizePhone(string? areaCode, string? number)
    {
        var area = Digits(areaCode, "El código de área");
        var local = Digits(number, "El número de teléfono");
        if (area is null && local is null) return (null, null);
        if (area is null || local is null)
            throw new ArgumentException("Complete el código de área y el número de teléfono.");
        if (area.Length is < 2 or > 4)
            throw new ArgumentException("El código de área tiene entre 2 y 4 dígitos, sin el 0 (por ejemplo 351).");
        if (local.Length is < 6 or > 8)
            throw new ArgumentException("El número tiene entre 6 y 8 dígitos, sin el 15.");
        if (area.Length + local.Length != 10)
            throw new ArgumentException("El código de área y el número juntos deben sumar 10 dígitos (por ejemplo 351 y 5551234).");
        return (area, local);
    }

    private static string? Digits(string? value, string field)
    {
        var trimmed = value?.Trim().Replace(" ", "").Replace("-", "");
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (!trimmed.All(char.IsAsciiDigit)) throw new ArgumentException($"{field} solo admite números.");
        return trimmed;
    }
}
