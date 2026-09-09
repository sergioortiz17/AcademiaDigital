using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AdmissionDocumentPolicyTests
{
    private readonly AdmissionDocumentPolicy _policy = new();

    [Fact]
    public void EnsureRequiredDocumentsApproved_reports_every_missing_requirement()
    {
        var required = new[]
        {
            new DocumentRequirement { Id = 1, Code = "DNI", IsRequired = true },
            new DocumentRequirement { Id = 2, Code = "TITULO", IsRequired = true }
        };
        var documents = new[]
        {
            new AdmissionApplicationDocument
            {
                DocumentRequirementId = 1,
                Status = StudentDocumentStatus.Approved
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _policy.EnsureRequiredDocumentsApproved(required, documents));

        Assert.DoesNotContain("DNI", exception.Message);
        Assert.Contains("TITULO", exception.Message);
    }

    [Fact]
    public void EnsureRequiredDocumentsApproved_ignores_submitted_or_rejected_versions()
    {
        var requirement = new DocumentRequirement { Id = 1, Code = "DNI", IsRequired = true };

        Assert.Throws<InvalidOperationException>(() => _policy.EnsureRequiredDocumentsApproved(
            [requirement],
            [
                new AdmissionApplicationDocument { DocumentRequirementId = 1, Status = StudentDocumentStatus.Submitted },
                new AdmissionApplicationDocument { DocumentRequirementId = 1, Status = StudentDocumentStatus.Rejected }
            ]));
    }

    [Fact]
    public void EnsureRequirementApplies_rejects_a_requirement_from_another_career()
        => Assert.Throws<InvalidOperationException>(() => _policy.EnsureRequirementApplies(
            new DocumentRequirement { CareerId = 9, IsActive = true },
            careerId: 7,
            new DateOnly(2026, 8, 22)));

    [Fact]
    public void EnsureCanReview_requires_an_observation_for_rejection()
        => Assert.Throws<ArgumentException>(() => _policy.EnsureCanReview(
            StudentDocumentStatus.Submitted,
            StudentDocumentStatus.Rejected,
            null));
}
