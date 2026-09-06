using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class TeacherDocumentPolicy
{
    private const long MaximumFileSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    public string NormalizeDocumentType(string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("El tipo de documento es obligatorio.");
        var normalized = documentType.Trim().ToUpperInvariant();
        if (normalized.Length > 50 || normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("El tipo de documento solo puede contener letras, números, '-' o '_'.");
        return normalized;
    }

    public void ValidateSubmission(
        string fileUrl,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        DateOnly? validUntil,
        DateOnly today)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != "storage"))
            throw new ArgumentException("La URL del archivo debe usar HTTPS o una URI lógica de almacenamiento.");
        if (string.IsNullOrWhiteSpace(originalFileName)
            || originalFileName.Contains('/')
            || originalFileName.Contains('\\'))
            throw new ArgumentException("El nombre original del archivo no es válido.");
        if (!AllowedContentTypes.Contains(contentType.Trim()))
            throw new ArgumentException("El tipo de contenido debe ser PDF, JPEG o PNG.");
        if (fileSizeBytes is <= 0 or > MaximumFileSizeBytes)
            throw new ArgumentException("El tamaño del archivo debe estar entre 1 byte y 10 MB.");
        if (validUntil.HasValue && validUntil.Value < today)
            throw new ArgumentException("La validez del documento no puede finalizar en el pasado.");
    }

    public void EnsureCanReview(
        StudentDocumentStatus currentStatus,
        StudentDocumentStatus targetStatus,
        string? observation)
    {
        if (currentStatus != StudentDocumentStatus.Submitted)
            throw new InvalidOperationException("Solo se pueden revisar los documentos docentes enviados.");
        if (targetStatus is not (StudentDocumentStatus.Approved or StudentDocumentStatus.Rejected))
            throw new ArgumentException("El estado de revisión debe ser Aprobado o Rechazado.");
        if (targetStatus == StudentDocumentStatus.Rejected && string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("La observación es obligatoria al rechazar un documento.");
    }
}
