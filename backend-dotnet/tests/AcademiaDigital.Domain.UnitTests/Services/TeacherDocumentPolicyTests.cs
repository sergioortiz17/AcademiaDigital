using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class TeacherDocumentPolicyTests
{
    private readonly TeacherDocumentPolicy policy = new();
    private static readonly DateOnly Today = new(2027, 3, 10);

    [Fact]
    public void NormalizeDocumentType_trims_and_normalizes_a_safe_code()
        => Assert.Equal("CV_DOCENTE", policy.NormalizeDocumentType(" cv_docente "));

    [Fact]
    public void NormalizeDocumentType_rejects_path_characters()
        => Assert.Throws<ArgumentException>(() => policy.NormalizeDocumentType("cv/docente"));

    [Fact]
    public void ValidateSubmission_accepts_https_pdf_metadata()
        => policy.ValidateSubmission(
            "https://files.example.edu/teacher/cv.pdf",
            "cv.pdf",
            "application/pdf",
            1024,
            Today.AddYears(1),
            Today);

    [Fact]
    public void ValidateSubmission_rejects_unsafe_or_invalid_metadata()
    {
        Assert.Throws<ArgumentException>(() => policy.ValidateSubmission(
            "file:///tmp/cv.pdf", "cv.pdf", "application/pdf", 1024, null, Today));
        Assert.Throws<ArgumentException>(() => policy.ValidateSubmission(
            "https://files.example.edu/cv.exe", "../cv.exe", "application/octet-stream", 0, null, Today));
        Assert.Throws<ArgumentException>(() => policy.ValidateSubmission(
            "https://files.example.edu/cv.pdf", "cv.pdf", "application/pdf", 1024, Today.AddDays(-1), Today));
    }

    [Fact]
    public void EnsureCanReview_requires_a_submitted_document_and_rejection_observation()
    {
        policy.EnsureCanReview(StudentDocumentStatus.Submitted, StudentDocumentStatus.Approved, null);
        Assert.Throws<ArgumentException>(() => policy.EnsureCanReview(
            StudentDocumentStatus.Submitted, StudentDocumentStatus.Rejected, null));
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanReview(
            StudentDocumentStatus.Approved, StudentDocumentStatus.Rejected, "Correction"));
    }
}
