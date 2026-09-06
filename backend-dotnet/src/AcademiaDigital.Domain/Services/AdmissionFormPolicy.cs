using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionFormPolicy
{
    public string NormalizeSlug(string slug)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length is < 3 or > 100
            || normalized[0] == '-'
            || normalized[^1] == '-'
            || normalized.Any(character => !char.IsLower(character) && !char.IsDigit(character) && character != '-'))
            throw new ArgumentException("El slug del formulario de admisión no es válido.");
        return normalized;
    }

    public void ValidateDefinition(
        string title,
        string termsText,
        int reservationHours,
        IReadOnlyCollection<AdmissionFormField> fields)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            throw new ArgumentException("El título del formulario de admisión es obligatorio y no puede superar los 200 caracteres.");
        if (string.IsNullOrWhiteSpace(termsText) || termsText.Trim().Length > 8000)
            throw new ArgumentException("Los términos del formulario de admisión son obligatorios y no pueden superar los 8000 caracteres.");
        if (reservationHours is < 1 or > 720)
            throw new ArgumentException("Las horas de reserva de admisión deben estar entre 1 y 720.");
        if (fields.Count is < 2 or > 50)
            throw new ArgumentException("El formulario de admisión debe contener entre 2 y 50 campos.");

        var duplicateKey = fields
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateKey is not null)
            throw new ArgumentException($"La clave de campo de admisión '{duplicateKey}' está duplicada.");
        if (fields.GroupBy(field => field.SortOrder).Any(group => group.Count() > 1))
            throw new ArgumentException("Los órdenes de los campos de admisión deben ser únicos.");

        foreach (var field in fields)
        {
            var key = field.Key.Trim();
            if (!Enum.IsDefined(field.Type))
                throw new ArgumentException($"El campo de admisión '{key}' tiene un tipo no válido.");
            if (key.Length is < 2 or > 100
                || !char.IsLower(key[0])
                || key.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
                throw new ArgumentException($"La clave de campo de admisión '{field.Key}' no es válida.");
            if (string.IsNullOrWhiteSpace(field.Label) || field.Label.Trim().Length > 150)
                throw new ArgumentException($"El campo de admisión '{key}' debe tener una etiqueta válida.");
            if (field.SortOrder < 0)
                throw new ArgumentException($"El campo de admisión '{key}' tiene un orden no válido.");
        }

        EnsureRequiredIdentityField(fields, "email", AdmissionFieldType.Email);
        EnsureRequiredIdentityField(fields, "dni", AdmissionFieldType.Text);
    }

    private static void EnsureRequiredIdentityField(
        IEnumerable<AdmissionFormField> fields,
        string key,
        AdmissionFieldType expectedType)
    {
        var field = fields.SingleOrDefault(candidate =>
            string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase));
        if (field is null || !field.IsRequired || field.Type != expectedType)
            throw new ArgumentException($"El formulario de admisión debe contener el campo obligatorio '{key}' con tipo {expectedType}.");
    }
}
