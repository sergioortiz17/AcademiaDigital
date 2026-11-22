using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Application.UseCases.Authentication;

public class RegisterUseCase(IUserRepository userRepository, IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository, ICareerRepository careerRepository,
    IStudyPlanRepository studyPlanRepository, IStudentAcademicRepository studentAcademicRepository,
    ITeacherRepository teacherRepository, IUnitOfWork unitOfWork)
{
    public const string PendingMessage =
        "Recibimos tu registro. La institución tiene que darte de alta antes de que puedas iniciar sesión.";

    /// <summary>
    /// Registro público (pantalla "Crear cuenta"). La cuenta queda pendiente de alta por un Admin
    /// (sección "Aspirantes"). Si el email es del dominio docente de la institución, se crea como
    /// Profesor con su perfil de docente inactivo y sin carrera; si no, como Alumno de la carrera.
    /// </summary>
    public async Task<RegisterResult> ExecutePublicAsync(
        string email, string name, string lastName, string password, string dni, int? careerId,
        string? teacherEmailDomain, CancellationToken ct = default)
    {
        if (TeacherEmailDomains.Matches(email, teacherEmailDomain))
            return await RegisterTeacherAsync(email, name, lastName, password, dni, ct);

        if (careerId is not int career)
            throw new ArgumentException("Elige la carrera a la que quieres inscribirte.");

        return await RegisterStudentAsync(email, name, lastName, password, dni, career, pending: true, ct);
    }

    /// <summary>Alta directa de un Alumno ya aprobado (DevTools y escenarios de prueba).</summary>
    public Task<RegisterResult> ExecuteAsync(
        string email, string name, string lastName, string password, string dni, int careerId,
        CancellationToken ct = default)
        => RegisterStudentAsync(email, name, lastName, password, dni, careerId, pending: false, ct);

    private async Task<RegisterResult> RegisterTeacherAsync(
        string email, string name, string lastName, string password, string dni, CancellationToken ct)
    {
        var normalizedEmail = email.Trim();
        var normalizedDni = dni.Trim();
        await EnsureEmailAndDniAreFreeAsync(normalizedEmail, normalizedDni, ct);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var user = await userRepository.CreateAsync(normalizedEmail, name, lastName, password, normalizedDni,
                UserRole.Profesor, transactionCt);
            await MarkPendingAsync(user, transactionCt);
            // Perfil docente inactivo: no aparece para asignar materias hasta que el Admin lo da de alta.
            await teacherRepository.CreateAsync(new Teacher
            {
                UserId = user.Id,
                EmployeeNumber = $"DOC-{user.Id:D5}",
                HireDate = DateTime.UtcNow.Date,
                IsActive = false
            }, transactionCt);
            return new RegisterResult(true, user.Id, PendingMessage, UserRole.Profesor, PendingApproval: true);
        }, ct);
    }

    /// <summary>
    /// Aspirante importado desde un listado (prematriculados del ministerio): queda pendiente de
    /// alta como en el registro público. La contraseña es aleatoria; la persona entra con
    /// "¿Olvidaste tu contraseña?" o con Google.
    /// </summary>
    public Task<RegisterResult> ImportApplicantAsync(
        string email, string name, string lastName, string dni, int careerId, CancellationToken ct = default)
        => RegisterStudentAsync(email, name, lastName,
            Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)) + "aA1!",
            dni, careerId, pending: true, ct);

    private async Task<RegisterResult> RegisterStudentAsync(
        string email, string name, string lastName, string password, string dni, int careerId,
        bool pending, CancellationToken ct)
    {
        var normalizedEmail = email.Trim();
        var normalizedDni = dni.Trim();

        var career = await careerRepository.FindByIdAsync(careerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");
        if (!career.IsActive) throw new InvalidOperationException("La carrera está inactiva.");

        // El alumno necesita un plan de estudios vigente para poder inscribirse. Si la carrera no
        // tiene un StudyPlan Active, fallamos el registro con un mensaje claro (en vez de dejar al
        // alumno sin plan y que el error recién aparezca en su primera inscripción).
        var activePlan = await studyPlanRepository.GetActiveByCareerIdAsync(career.Id, ct)
            ?? throw new InvalidOperationException(
                "La carrera no tiene un plan de estudios vigente. Un administrador debe publicar uno antes de que los alumnos puedan registrarse en ella.");

        await EnsureEmailAndDniAreFreeAsync(normalizedEmail, normalizedDni, ct);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var user = await userRepository.CreateAsync(normalizedEmail, name, lastName, password, normalizedDni,
                UserRole.Alumno, transactionCt);
            if (pending) await MarkPendingAsync(user, transactionCt);
            var enrolledAt = DateTime.UtcNow;
            var student = await studentRepository.CreateAsync(new Student
            {
                UserId = user.Id,
                CareerId = career.Id,
                LegajoNumber = $"{enrolledAt.Year}-{user.Id:D5}",
                EnrollmentDate = enrolledAt,
                Status = StudentStatus.Regular
            }, transactionCt);
            var membership = await studentCareerRepository.CreateAsync(new StudentCareer
            {
                StudentId = student.Id,
                CareerId = career.Id,
                EnrollmentDate = enrolledAt,
                AdmissionYear = enrolledAt.Year
            }, transactionCt);
            // Plan de estudios actual del alumno (IsCurrent=true). Sin esto, la primera inscripción
            // fallaba con "El alumno no tiene un plan de estudios actual para la carrera...".
            await studentAcademicRepository.AssignStudyPlanAsync(new StudentStudyPlan
            {
                StudentId = student.Id,
                StudentCareerId = membership.Id,
                StudyPlanId = activePlan.Id
            }, transactionCt);
            return pending
                ? new RegisterResult(true, user.Id, PendingMessage, UserRole.Alumno, PendingApproval: true)
                : new RegisterResult(true, user.Id, "The user was successfully registered", UserRole.Alumno, PendingApproval: false);
        }, ct);
    }

    private async Task EnsureEmailAndDniAreFreeAsync(string email, string dni, CancellationToken ct)
    {
        if (await userRepository.FindByEmailAsync(email, ct) != null)
            throw new EmailAlreadyExistsException();
        if (await userRepository.FindByDniAsync(dni, ct) != null)
            throw new DniAlreadyExistsException();
    }

    private async Task MarkPendingAsync(Domain.Entities.User user, CancellationToken ct)
    {
        user.RegistrationStatus = RegistrationStatus.Pending;
        await userRepository.UpdateAsync(user, ct);
    }
}

public record RegisterResult(bool Success, long UserId, string Msg, UserRole Role, bool PendingApproval);
