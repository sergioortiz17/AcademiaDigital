using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class RegisterUseCase(IUserRepository userRepository, IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository, ICareerRepository careerRepository, IUnitOfWork unitOfWork)
{
    public async Task<RegisterResult> ExecuteAsync(
        string email, string name, string lastName, string password, string dni, int careerId,
        CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim();
        var normalizedDni = dni.Trim();

        var career = await careerRepository.FindByIdAsync(careerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");
        if (!career.IsActive) throw new InvalidOperationException("La carrera está inactiva.");

        var existingEmail = await userRepository.FindByEmailAsync(normalizedEmail, ct);
        if (existingEmail != null)
            throw new EmailAlreadyExistsException();

        var existingDni = await userRepository.FindByDniAsync(normalizedDni, ct);
        if (existingDni != null)
            throw new DniAlreadyExistsException();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var user = await userRepository.CreateAsync(normalizedEmail, name, lastName, password, normalizedDni,
                UserRole.Alumno, transactionCt);
            var enrolledAt = DateTime.UtcNow;
            var student = await studentRepository.CreateAsync(new Student
            {
                UserId = user.Id,
                CareerId = career.Id,
                LegajoNumber = $"{enrolledAt.Year}-{user.Id:D5}",
                EnrollmentDate = enrolledAt,
                Status = StudentStatus.Regular
            }, transactionCt);
            await studentCareerRepository.CreateAsync(new StudentCareer
            {
                StudentId = student.Id,
                CareerId = career.Id,
                EnrollmentDate = enrolledAt
            }, transactionCt);
            return new RegisterResult(true, user.Id, "The user was successfully registered");
        }, ct);
    }
}

public record RegisterResult(bool Success, long UserId, string Msg);
