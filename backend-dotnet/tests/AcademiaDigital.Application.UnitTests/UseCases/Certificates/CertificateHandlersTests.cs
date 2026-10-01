using System.Text;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Certificates;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Certificates;

public sealed class CertificateHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_normalizes_legacy_type_and_targets_active_career()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        repository.GetAcademicRecordAsync(7, null, null, Arg.Any<CancellationToken>()).Returns(Academic());
        repository.CreateAsync(Arg.Any<CertificateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => { var request = call.Arg<CertificateRequest>(); request.Id = 12; return request; });
        var handler = new CreateCertificateRequestUseCase(
            repository, new CertificatePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.ExecuteAsync(7, "Certificado de alumno regular", ct: TestContext.Current.CancellationToken);

        Assert.Equal("RegularStudent", result.Kind);
        Assert.Equal(20, result.StudentCareerId);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task Create_rejects_duplicate_active_request()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        repository.GetAcademicRecordAsync(7, null, null, Arg.Any<CancellationToken>()).Returns(Academic());
        repository.HasActiveRequestAsync(7, 20, CertificateKind.RegularStudent, null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateCertificateRequestUseCase(
            repository, new CertificatePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            7, "RegularStudent", ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Admin_can_approve_pending_request_with_audit()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        var request = Request(CertificateStatus.Pending);
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(request);
        var handler = new ReviewCertificateRequestCommandHandler(
            repository, new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new ReviewCertificateRequestCommand(10, true, null, 99), TestContext.Current.CancellationToken);

        Assert.Equal("Approved", result.Status);
        Assert.Equal(99, request.ReviewedByUserId);
        Assert.Equal(Now.UtcDateTime, request.ReviewedAt);
    }

    [Fact]
    public async Task Issue_reserves_sequence_generates_pdf_and_marks_request_issued()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        var request = Request(CertificateStatus.Approved);
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(request);
        repository.GetAcademicRecordAsync(7, 20, null, Arg.Any<CancellationToken>()).Returns(Academic());
        repository.LockSequenceAsync(Arg.Any<CancellationToken>()).Returns(new CertificateSequence());
        var pdf = Substitute.For<ICertificatePdfGenerator>();
        pdf.GenerateAsync(Arg.Any<CertificatePdfModel>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.ASCII.GetBytes("%PDF-1.4 certificate"));
        var storage = Substitute.For<IFileStorage>();
        storage.SaveAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), "application/pdf", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(0));
        var handler = new IssueCertificateCommandHandler(
            repository, new CertificatePolicy(), new ImmediateUnitOfWork(), pdf, storage, new FixedTimeProvider(Now));

        var result = await handler.Handle(new IssueCertificateCommand(10, 99), TestContext.Current.CancellationToken);

        Assert.Equal("CERT-00000001", result.CertificateNumber);
        Assert.Equal("Ready", result.Status);
        Assert.Equal(CertificateStatus.Issued, request.Status);
        Assert.Equal(64, result.Sha256!.Length);
        repository.Received(1).AddIssuance(Arg.Is<CertificateIssuance>(item => item.SequenceNumber == 1));
    }

    [Fact]
    public async Task Repeated_issue_of_ready_certificate_is_idempotent()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        var request = Request(CertificateStatus.Issued);
        var issuance = ReadyIssuance(request);
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(request);
        repository.FindIssuanceByRequestAsync(10, true, Arg.Any<CancellationToken>()).Returns(issuance);
        var pdf = Substitute.For<ICertificatePdfGenerator>();
        var handler = new IssueCertificateCommandHandler(
            repository, new CertificatePolicy(), new ImmediateUnitOfWork(), pdf,
            Substitute.For<IFileStorage>(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new IssueCertificateCommand(10, 99), TestContext.Current.CancellationToken);

        Assert.Equal("CERT-00000001", result.CertificateNumber);
        await pdf.DidNotReceive().GenerateAsync(Arg.Any<CertificatePdfModel>(), Arg.Any<CancellationToken>());
        repository.DidNotReceive().AddIssuance(Arg.Any<CertificateIssuance>());
    }

    [Fact]
    public async Task Download_rejects_another_student()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        var issuance = ReadyIssuance(Request(CertificateStatus.Issued));
        repository.FindIssuanceByPublicIdAsync(issuance.PublicId, Arg.Any<CancellationToken>()).Returns(issuance);
        var handler = new DownloadCertificateQueryHandler(repository, Substitute.For<IFileStorage>());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new DownloadCertificateQuery(issuance.PublicId, 8, false), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Download_validates_stored_file_hash()
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        var issuance = ReadyIssuance(Request(CertificateStatus.Issued));
        issuance.Sha256 = new string('0', 64);
        repository.FindIssuanceByPublicIdAsync(issuance.PublicId, Arg.Any<CancellationToken>()).Returns(issuance);
        var storage = Substitute.For<IFileStorage>();
        storage.ReadAsync("certificates/a.pdf", "application/pdf", "CERT-00000001.pdf", Arg.Any<CancellationToken>())
            .Returns(new StoredFile(Encoding.ASCII.GetBytes("changed"), "application/pdf", "CERT-00000001.pdf"));
        var handler = new DownloadCertificateQueryHandler(repository, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new DownloadCertificateQuery(issuance.PublicId, 7, false), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(CertificateStatus.Issuing)]
    [InlineData(CertificateStatus.Issued)]
    public async Task Legacy_request_contract_projects_internal_issue_states_as_approved(CertificateStatus internalStatus)
    {
        var repository = Substitute.For<ICertificateRequestRepository>();
        repository.GetByUserAsync(7, Arg.Any<CancellationToken>())
            .Returns([Request(internalStatus)]);
        var handler = new GetCertificateRequestsUseCase(repository);

        var result = await handler.ExecuteAsync(7, TestContext.Current.CancellationToken);

        Assert.Equal("Approved", result.Single().Status);
    }

    private static CertificateAcademicRecord Academic() => new(
        30, 20, true, StudentStatus.Regular, "LEG-30", "12345678", "Ada Lovelace", "Sistemas",
        [new CertificateCourseRecord(1, "MAT", "Matemática", 2026, 1, EnrollmentStatus.Approved, 9m)], null);

    private static CertificateRequest Request(CertificateStatus status) => new()
    {
        Id = 10,
        UserId = 7,
        StudentCareerId = 20,
        Kind = CertificateKind.RegularStudent,
        CertificateType = "Certificado de alumno regular",
        Status = status,
        CreatedAt = Now.UtcDateTime
    };

    private static CertificateIssuance ReadyIssuance(CertificateRequest request) => new()
    {
        PublicId = Guid.NewGuid(),
        CertificateRequestId = request.Id,
        CertificateRequest = request,
        SequenceNumber = 1,
        CertificateNumber = "CERT-00000001",
        Status = CertificateIssuanceStatus.Ready,
        FileName = "CERT-00000001.pdf",
        ContentType = "application/pdf",
        StorageKey = "certificates/a.pdf",
        Sha256 = new string('A', 64),
        SnapshotJson = "{}",
        CreatedAt = Now.UtcDateTime,
        GeneratedAt = Now.UtcDateTime,
        IssuedByUserId = 99
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
