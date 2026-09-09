using System.Net.Mail;
using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionApplicationPolicy
{
    public IReadOnlyDictionary<string, string> ValidateAndNormalize(
        AdmissionForm form,
        IReadOnlyDictionary<string, string?> submittedFields,
        bool acceptedTerms)
    {
        if (!form.IsActive)
            throw new InvalidOperationException("El formulario de admisión no está activo.");
        if (!acceptedTerms)
            throw new ArgumentException("Se deben aceptar los términos de admisión.");

        var configuredFields = form.Fields.ToDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, rawValue) in submittedFields)
        {
            if (!configuredFields.TryGetValue(key, out var configuredField))
                throw new ArgumentException($"El campo '{key}' no está configurado para este formulario de admisión.");

            var value = rawValue?.Trim();
            if (!string.IsNullOrEmpty(value))
                normalized[configuredField.Key] = value;
        }

        var missing = form.Fields
            .Where(field => field.IsRequired && !normalized.ContainsKey(field.Key))
            .OrderBy(field => field.SortOrder)
            .Select(field => field.Key)
            .ToArray();
        if (missing.Length > 0)
            throw new ArgumentException($"Faltan campos de admisión obligatorios: {string.Join(", ", missing)}.");

        var email = RequiredValue(normalized, "email").ToLowerInvariant();
        try
        {
            var parsedEmail = new MailAddress(email);
            if (!string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
                throw new FormatException();
        }
        catch (FormatException)
        {
            throw new ArgumentException("El email de admisión no es válido.");
        }

        var dni = RequiredValue(normalized, "dni");
        if (dni.Length is < 7 or > 8 || dni.Any(character => !char.IsDigit(character)))
            throw new ArgumentException("El DNI de admisión no es válido.");

        normalized["email"] = email;
        normalized["dni"] = dni;
        return normalized;
    }

    private static string RequiredValue(IReadOnlyDictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"El formulario de admisión debe configurar y requerir el campo '{key}'.");
}
