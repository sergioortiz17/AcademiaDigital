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
            throw new ArgumentException("Document type is required.");
        var normalized = documentType.Trim().ToUpperInvariant();
        if (normalized.Length > 50 || normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Document type must contain only letters, numbers, '-' or '_'.");
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
            throw new ArgumentException("File URL must use HTTPS or a logical storage URI.");
        if (string.IsNullOrWhiteSpace(originalFileName)
            || originalFileName.Contains('/')
            || originalFileName.Contains('\\'))
            throw new ArgumentException("Original file name is invalid.");
        if (!AllowedContentTypes.Contains(contentType.Trim()))
            throw new ArgumentException("Content type must be PDF, JPEG or PNG.");
        if (fileSizeBytes is <= 0 or > MaximumFileSizeBytes)
            throw new ArgumentException("File size must be between 1 byte and 10 MB.");
        if (validUntil.HasValue && validUntil.Value < today)
            throw new ArgumentException("Document validity cannot end in the past.");
    }

    public void EnsureCanReview(
        StudentDocumentStatus currentStatus,
        StudentDocumentStatus targetStatus,
        string? observation)
    {
        if (currentStatus != StudentDocumentStatus.Submitted)
            throw new InvalidOperationException("Only submitted teacher documents can be reviewed.");
        if (targetStatus is not (StudentDocumentStatus.Approved or StudentDocumentStatus.Rejected))
            throw new ArgumentException("Review status must be Approved or Rejected.");
        if (targetStatus == StudentDocumentStatus.Rejected && string.IsNullOrWhiteSpace(observation))
            throw new ArgumentException("Observation is required when rejecting a document.");
    }
}
