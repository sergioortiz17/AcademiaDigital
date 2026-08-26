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
            throw new ArgumentException("Admission form slug is invalid.");
        return normalized;
    }

    public void ValidateDefinition(
        string title,
        string termsText,
        int reservationHours,
        IReadOnlyCollection<AdmissionFormField> fields)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            throw new ArgumentException("Admission form title is required and cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(termsText) || termsText.Trim().Length > 8000)
            throw new ArgumentException("Admission form terms are required and cannot exceed 8000 characters.");
        if (reservationHours is < 1 or > 720)
            throw new ArgumentException("Admission reservation hours must be between 1 and 720.");
        if (fields.Count is < 2 or > 50)
            throw new ArgumentException("Admission form must contain between 2 and 50 fields.");

        var duplicateKey = fields
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateKey is not null)
            throw new ArgumentException($"Admission field key '{duplicateKey}' is duplicated.");
        if (fields.GroupBy(field => field.SortOrder).Any(group => group.Count() > 1))
            throw new ArgumentException("Admission field sort orders must be unique.");

        foreach (var field in fields)
        {
            var key = field.Key.Trim();
            if (!Enum.IsDefined(field.Type))
                throw new ArgumentException($"Admission field '{key}' has an invalid type.");
            if (key.Length is < 2 or > 100
                || !char.IsLower(key[0])
                || key.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
                throw new ArgumentException($"Admission field key '{field.Key}' is invalid.");
            if (string.IsNullOrWhiteSpace(field.Label) || field.Label.Trim().Length > 150)
                throw new ArgumentException($"Admission field '{key}' must have a valid label.");
            if (field.SortOrder < 0)
                throw new ArgumentException($"Admission field '{key}' has an invalid sort order.");
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
            throw new ArgumentException($"Admission form must contain required field '{key}' with type {expectedType}.");
    }
}
