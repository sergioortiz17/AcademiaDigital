using System.Text;
using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;
using UserEntity = AcademiaDigital.Domain.Entities.User;

namespace AcademiaDigital.Application.UnitTests.UseCases.Students;

public sealed class StudentAdmissionDocumentsUseCaseTests
{
    private readonly IStudentRepository students = Substitute.For<IStudentRepository>();
    private readonly IStudentManagementService service = Substitute.For<IStudentManagementService>();
    private readonly IStudentDocumentStorage storage = Substitute.For<IStudentDocumentStorage>();

    [Fact]
    public async Task Uploads_go_to_a_readable_folder_per_student_and_requirement()
    {
        students.FindByUserIdAsync(9, Arg.Any<CancellationToken>()).Returns(new Student
        {
            Id = 3, CareerId = 1, LegajoNumber = "2026-00003",
            User = new UserEntity { Dni = "40123456", LastName = "Pérez", Username = "Ana María" }
        });
        service.GetRequirementsAsync(1, Arg.Any<CancellationToken>()).Returns([
            new DocumentRequirementDto(5, "ANALITICO", "Analítico", null, null, true, true, null, null)]);
        storage.SaveAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<string>(0));
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 11, 22, 15, 30, 0, TimeSpan.Zero));

        await new StudentAdmissionDocumentsUseCase(students, service, storage, clock)
            .UploadMineAsync(9, 5, "Analítico secundario.pdf", Encoding.ASCII.GetBytes("%PDF-1.7"), TestContext.Current.CancellationToken);

        await storage.Received(1).SaveAsync("40123456_Perez_Ana_Maria/ANALITICO/20261122-153000_Analitico_secundario.pdf",
            Arg.Any<ReadOnlyMemory<byte>>(), "application/pdf", "Analítico secundario.pdf", Arg.Any<CancellationToken>());
        await service.Received(1).AddDocumentAsync(3, Arg.Is<CreateStudentDocumentRequest>(r =>
            r.FileUrl == "40123456_Perez_Ana_Maria/ANALITICO/20261122-153000_Analitico_secundario.pdf"), Arg.Any<CancellationToken>());
    }
}
