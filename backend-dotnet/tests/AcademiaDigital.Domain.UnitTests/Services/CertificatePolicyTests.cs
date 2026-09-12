using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class CertificatePolicyTests
{
    private readonly CertificatePolicy policy = new();

    [Theory]
    [InlineData("Certificado de alumno regular", CertificateKind.RegularStudent)]
    [InlineData("Constancia de inscripción", CertificateKind.Enrollment)]
    [InlineData("Constancia de matrícula", CertificateKind.Enrollment)]
    [InlineData("Certificado de materias aprobadas", CertificateKind.ApprovedCourses)]
    [InlineData("Certificado de promedio", CertificateKind.AcademicStatus)]
    [InlineData("Certificado analítico", CertificateKind.Transcript)]
    [InlineData("Constancia de egreso", CertificateKind.GeneralAcademicStatus)]
    [InlineData("Permiso de examen", CertificateKind.ExamPermit)]
    public void Legacy_and_m8_names_are_normalized(string value, CertificateKind expected)
        => Assert.Equal(expected, policy.ParseKind(value));

    [Fact]
    public void Unknown_certificate_type_is_rejected()
        => Assert.Throws<ArgumentException>(() => policy.ParseKind("Certificado inventado"));

    [Fact]
    public void Regular_certificate_requires_regular_student()
        => Assert.Throws<InvalidOperationException>(() => policy.EnsureEligible(
            CertificateKind.RegularStudent, Record(status: StudentStatus.Libre), null));

    [Fact]
    public void Enrollment_certificate_requires_an_active_enrollment()
        => Assert.Throws<InvalidOperationException>(() => policy.EnsureEligible(
            CertificateKind.Enrollment,
            Record(courses: [Course(EnrollmentStatus.Withdrawn)]), null));

    [Fact]
    public void Approved_courses_certificate_requires_an_approved_or_promoted_course()
    {
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEligible(
            CertificateKind.ApprovedCourses,
            Record(courses: [Course(EnrollmentStatus.Regularized)]), null));
        policy.EnsureEligible(CertificateKind.ApprovedCourses,
            Record(courses: [Course(EnrollmentStatus.Promoted)]), null);
    }

    [Fact]
    public void Exam_permit_requires_matching_open_registration()
    {
        var record = Record(exam: new CertificateExamRecord(
            44, "MAT", "Matemática", DateTime.UtcNow, "Aula 1", 1, ExamTableStatus.Open));
        Assert.Throws<ArgumentException>(() => policy.EnsureEligible(CertificateKind.ExamPermit, record, null));
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEligible(CertificateKind.ExamPermit, record, 45));
        policy.EnsureEligible(CertificateKind.ExamPermit, record, 44);
    }

    [Fact]
    public void Inactive_student_career_blocks_every_certificate()
        => Assert.Throws<InvalidOperationException>(() => policy.EnsureEligible(
            CertificateKind.Transcript, Record(active: false), null));

    [Fact]
    public void Mandatory_identity_data_cannot_be_missing()
    {
        var record = Record() with { Dni = string.Empty };
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEligible(
            CertificateKind.AcademicStatus, record, null));
    }

    [Fact]
    public void Review_and_issue_transitions_are_guarded()
    {
        var request = new CertificateRequest();
        request.Approve(2, DateTime.UtcNow);
        Assert.Equal(CertificateStatus.Approved, request.Status);
        Assert.Throws<InvalidOperationException>(() => request.Reject(2, "No", DateTime.UtcNow));
        request.MarkIssuing(DateTime.UtcNow);
        request.MarkIssued(DateTime.UtcNow);
        Assert.Equal(CertificateStatus.Issued, request.Status);
    }

    [Fact]
    public void Sequence_is_monotonic_and_issuance_keeps_integrity_metadata()
    {
        var sequence = new CertificateSequence();
        Assert.Equal(1, sequence.TakeNext());
        Assert.Equal(2, sequence.TakeNext());
        var issuance = new CertificateIssuance();
        issuance.MarkReady("certificates/a.pdf", new string('A', 64), DateTime.UtcNow);
        Assert.Equal(CertificateIssuanceStatus.Ready, issuance.Status);
        Assert.Equal(64, issuance.Sha256!.Length);
    }

    private static CertificateAcademicRecord Record(
        bool active = true,
        StudentStatus status = StudentStatus.Regular,
        IReadOnlyList<CertificateCourseRecord>? courses = null,
        CertificateExamRecord? exam = null)
        => new(10, 20, active, status, "LEG-1", "123", "Ada Lovelace", "Sistemas", courses ?? [], exam);

    private static CertificateCourseRecord Course(EnrollmentStatus status)
        => new(1, "MAT", "Matemática", 2026, 1, status, status is EnrollmentStatus.Approved or EnrollmentStatus.Promoted ? 8 : null);
}
