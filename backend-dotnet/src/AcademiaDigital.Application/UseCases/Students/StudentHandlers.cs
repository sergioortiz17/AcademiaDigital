using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Students;

public sealed record GetStudentsQuery(int? CareerId);
public sealed record GetStudentByIdQuery(long StudentId);
public sealed record CreateStudentCommand(CreateStudentRequest Request, long ActorId);

public sealed class GetStudentsQueryHandler(
    IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository,
    IStudentAcademicRepository studentAcademicRepository)
{
    public async Task<IReadOnlyList<StudentDto>> Handle(GetStudentsQuery query, CancellationToken ct = default)
    {
        var students = query.CareerId.HasValue
            ? await studentRepository.GetByCareerAsync(query.CareerId.Value, ct)
            : await studentRepository.GetAllAsync(ct);

        var result = new List<StudentDto>();
        foreach (var student in students)
            result.Add(await StudentDtoMapper.MapAsync(student, studentCareerRepository, studentAcademicRepository, ct));
        return result;
    }
}

public sealed class GetStudentByIdQueryHandler(
    IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository,
    IStudentAcademicRepository studentAcademicRepository)
{
    public async Task<StudentDto> Handle(GetStudentByIdQuery query, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByIdAsync(query.StudentId, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        return await StudentDtoMapper.MapAsync(student, studentCareerRepository, studentAcademicRepository, ct);
    }
}

public sealed class CreateStudentCommandHandler(
    IUserRepository userRepository,
    ICareerRepository careerRepository,
    IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository,
    IStudentAcademicRepository studentAcademicRepository,
    IStudentManagementService management,
    IFinanceClient financeClient,
    IUnitOfWork unitOfWork)
{
    public async Task<StudentDto> Handle(CreateStudentCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var academicValues = new[]
        {
            request.StudyPlanId.HasValue,
            request.DivisionId.HasValue,
            request.AcademicYear.HasValue,
            request.YearNumber.HasValue
        };
        if (academicValues.Any(x => x) && !academicValues.All(x => x))
            throw new ArgumentException("StudyPlanId, DivisionId, AcademicYear y YearNumber deben proporcionarse juntos.");

        var user = await userRepository.FindByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        if (user.Role != UserRole.Alumno)
            throw new InvalidOperationException("Solo los usuarios con rol Alumno pueden vincularse como estudiantes.");

        var career = await careerRepository.FindByIdAsync(request.CareerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");
        if (!career.IsActive) throw new InvalidOperationException("La carrera está inactiva.");

        if (await studentRepository.FindByUserIdAsync(user.Id, ct) is not null)
            throw new InvalidOperationException("El usuario ya está vinculado a un alumno.");

        var legajo = request.LegajoNumber.Trim();
        if (await studentRepository.FindByLegajoAsync(legajo, ct) is not null)
            throw new InvalidOperationException("El número de legajo ya existe.");

        var status = Enum.Parse<StudentStatus>(request.Status, ignoreCase: true);
        var created = await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var student = await studentRepository.CreateAsync(new Student
            {
                UserId = user.Id,
                CareerId = career.Id,
                LegajoNumber = legajo,
                EnrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow,
                Status = status
            }, transactionCt);

            // El domicilio y el contacto de emergencia viven en la cuenta: solo se completan los
            // que vienen en la solicitud, para no borrar lo que el alumno ya cargó en su perfil.
            if (ApplyContact(user, request))
                await userRepository.UpdateAsync(user, transactionCt);

            var studentCareer = await studentCareerRepository.CreateAsync(new StudentCareer
            {
                StudentId = student.Id,
                CareerId = career.Id,
                EnrollmentDate = student.EnrollmentDate,
                AdmissionYear = student.EnrollmentDate.Year
            }, transactionCt);

            if (academicValues.All(x => x))
            {
                await management.AssignAcademicAsync(student.Id, new CreateAcademicAssignmentRequest(
                    career.Id, request.StudyPlanId!.Value, request.DivisionId!.Value,
                    request.AcademicYear!.Value, request.YearNumber!.Value, request.StudyPlanMigrationReason),
                    command.ActorId, transactionCt);
            }
            return (student, studentCareer);
        }, ct);

        var createdStudent = created.student;

        // Finance (ADR 0001): registrar la deuda de matriculación es fire-and-forget y NUNCA
        // bloquea. Se llama DESPUÉS de commitear la matriculación; si Finance falla, se ignora.
        // BillingPlanId va null: hoy el flujo de alta de alumno no resuelve un plan de facturación,
        // así que el cliente loguea y omite (el seam queda cableado para cuando exista un plan).
        await financeClient.TryGenerateMatriculationDebtsAsync(new GenerateMatriculationDebtsRequest(
            StudentId: createdStudent.Id,
            CareerId: career.Id,
            StudentCareerId: created.studentCareer.Id,
            AcademicYear: request.AcademicYear ?? DateTime.UtcNow.Year,
            BillingPlanId: null,
            ActorUserId: command.ActorId), ct);

        createdStudent.User = user;
        createdStudent.Career = career;
        return await StudentDtoMapper.MapAsync(createdStudent, studentCareerRepository, studentAcademicRepository, ct);
    }

    private static bool ApplyContact(Domain.Entities.User user, CreateStudentRequest request)
    {
        var changed = false;
        void Set(string? value, Action<string> apply)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            apply(value.Trim());
            changed = true;
        }
        Set(request.AddressLine, v => user.AddressLine = v);
        Set(request.City, v => user.City = v);
        Set(request.Province, v => user.Province = v);
        Set(request.PostalCode, v => user.PostalCode = v);
        Set(request.EmergencyContactName, v => user.EmergencyContactName = v);
        Set(request.EmergencyContactRelationship, v => user.EmergencyContactRelationship = v);
        Set(request.EmergencyContactPhone, v => user.EmergencyContactPhone = v);
        return changed;
    }
}

internal static class StudentDtoMapper
{
    public static async Task<StudentDto> MapAsync(Student student, IStudentCareerRepository careerRepository,
        IStudentAcademicRepository academicRepository, CancellationToken ct)
    {
        var memberships = await careerRepository.GetByStudentAsync(student.Id, ct);
        var plans = await academicRepository.GetCurrentStudyPlansByCareerAsync(student.Id, ct);
        plans.TryGetValue(student.CareerId, out var primaryPlan);
        return new StudentDto
        {
            Id = student.Id,
            UserId = student.UserId,
            UserEmail = student.User.Email,
            UserName = $"{student.User.Username} {student.User.LastName}".Trim(),
            CareerId = student.CareerId,
            CareerName = student.Career.Name,
            LegajoNumber = student.LegajoNumber,
            EnrollmentDate = student.EnrollmentDate,
            Status = student.Status.ToString(),
            CurrentStudyPlanId = primaryPlan?.StudyPlanId,
            CurrentStudyPlanName = primaryPlan?.StudyPlan.Name,
            Careers = memberships.Select(m =>
            {
                plans.TryGetValue(m.CareerId, out var plan);
                return new StudentCareerDto(m.Id, m.CareerId, m.Career.Name, m.EnrollmentDate, m.IsActive,
                    m.CareerId == student.CareerId, plan?.StudyPlanId, plan?.StudyPlan.Name);
            }).ToList()
        };
    }
}
