using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Teachers;

public sealed class TeacherDocumentHandlersTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Submit_normalizes_and_creates_a_version_in_a_serializable_transaction()
    {
        var repository = Substitute.For<ITeacherDocumentRepository>();
        var unitOfWork = SerializableUnitOfWork();
        repository.CreateVersionAsync(Arg.Any<TeacherDocument>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var document = call.Arg<TeacherDocument>();
                document.Id = 40;
                document.Version = 2;
                return document;
            });
        var handler = new SubmitTeacherDocumentCommandHandler(
            repository, new TeacherDocumentPolicy(), unitOfWork, new FixedTimeProvider(Now));

        var result = await handler.Handle(Command(), TestContext.Current.CancellationToken);

        Assert.Equal(40, result.Id);
        Assert.Equal("CV_DOCENTE", result.DocumentType);
        Assert.Equal(2, result.Version);
        Assert.Equal(Now.UtcDateTime, result.SubmittedAt);
        await unitOfWork.Received(1).ExecuteInSerializableTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<TeacherDocument>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_rejects_invalid_metadata_before_opening_a_transaction()
    {
        var repository = Substitute.For<ITeacherDocumentRepository>();
        var unitOfWork = SerializableUnitOfWork();
        var handler = new SubmitTeacherDocumentCommandHandler(
            repository, new TeacherDocumentPolicy(), unitOfWork, new FixedTimeProvider(Now));
        var invalid = Command() with { FileUrl = "file:///tmp/cv.pdf" };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(invalid, TestContext.Current.CancellationToken));

        await unitOfWork.DidNotReceive().ExecuteInSerializableTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<TeacherDocument>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Review_records_status_actor_time_and_trimmed_observation()
    {
        var repository = Substitute.For<ITeacherDocumentRepository>();
        var document = new TeacherDocument
        {
            Id = 40,
            TeacherId = 10,
            DocumentType = "CV_DOCENTE",
            Version = 1,
            FileUrl = "https://files.example.edu/cv.pdf",
            OriginalFileName = "cv.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            Status = StudentDocumentStatus.Submitted,
            SubmittedAt = Now.UtcDateTime
        };
        repository.FindAsync(10, 40, true, Arg.Any<CancellationToken>()).Returns(document);
        repository.UpdateAsync(document, Arg.Any<CancellationToken>()).Returns(document);
        var handler = new ReviewTeacherDocumentCommandHandler(
            repository, new TeacherDocumentPolicy(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new ReviewTeacherDocumentCommand(
            10, 40, StudentDocumentStatus.Rejected, " Incomplete ", 99),
            TestContext.Current.CancellationToken);

        Assert.Equal("Rejected", result.Status);
        Assert.Equal("Incomplete", result.Observation);
        Assert.Equal(99, result.ReviewedByUserId);
        Assert.Equal(Now.UtcDateTime, result.ReviewedAt);
    }

    [Fact]
    public async Task List_rejects_an_unknown_teacher_before_reading_documents()
    {
        var teachers = Substitute.For<ITeacherRepository>();
        var documents = Substitute.For<ITeacherDocumentRepository>();
        var handler = new GetTeacherDocumentsQueryHandler(teachers, documents);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetTeacherDocumentsQuery(10), TestContext.Current.CancellationToken));

        await documents.DidNotReceive().GetByTeacherAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    private static SubmitTeacherDocumentCommand Command() => new(
        10,
        " cv_docente ",
        "https://files.example.edu/teacher/cv.pdf",
        "cv.pdf",
        "application/pdf",
        1024,
        new DateOnly(2028, 3, 10));

    private static IUnitOfWork SerializableUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<TeacherDocument>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<TeacherDocument>>>()(
                call.ArgAt<CancellationToken>(1)));
        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
