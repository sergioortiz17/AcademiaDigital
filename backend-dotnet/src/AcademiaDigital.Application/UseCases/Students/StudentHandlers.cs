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
            ?? throw new KeyNotFoundException("Student not found.");
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
    IUnitOfWork unitOfWork)
{
    public async Task<StudentDto> Handle(CreateStudentCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var academicValues = new[]
        {
            request.StudyPlanId.HasValue,
            request.CommissionId.HasValue,
            request.AcademicYear.HasValue,
            request.YearNumber.HasValue
        };
        if (academicValues.Any(x => x) && !academicValues.All(x => x))
            throw new ArgumentException("StudyPlanId, CommissionId, AcademicYear and YearNumber must be provided together.");

        var user = await userRepository.FindByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        if (user.Role != UserRole.Alumno)
            throw new InvalidOperationException("Only users with Alumno role can be linked as students.");

        var career = await careerRepository.FindByIdAsync(request.CareerId, ct)
            ?? throw new KeyNotFoundException("Career not found.");
        if (!career.IsActive) throw new InvalidOperationException("Career is inactive.");

        if (await studentRepository.FindByUserIdAsync(user.Id, ct) is not null)
            throw new InvalidOperationException("User is already linked to a student.");

        var legajo = request.LegajoNumber.Trim();
        if (await studentRepository.FindByLegajoAsync(legajo, ct) is not null)
            throw new InvalidOperationException("Legajo number already exists.");

        var status = Enum.Parse<StudentStatus>(request.Status, ignoreCase: true);
        var created = await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var student = await studentRepository.CreateAsync(new Student
            {
                UserId = user.Id,
                CareerId = career.Id,
                LegajoNumber = legajo,
                EnrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow,
                Status = status,
                AddressLine = request.AddressLine?.Trim(),
                City = request.City?.Trim(),
                Province = request.Province?.Trim(),
                PostalCode = request.PostalCode?.Trim(),
                EmergencyContactName = request.EmergencyContactName?.Trim(),
                EmergencyContactRelationship = request.EmergencyContactRelationship?.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone?.Trim()
            }, transactionCt);

            await studentCareerRepository.CreateAsync(new StudentCareer
            {
                StudentId = student.Id,
                CareerId = career.Id,
                EnrollmentDate = student.EnrollmentDate
            }, transactionCt);

            if (academicValues.All(x => x))
            {
                await management.AssignAcademicAsync(student.Id, new CreateAcademicAssignmentRequest(
                    career.Id, request.StudyPlanId!.Value, request.CommissionId!.Value,
                    request.AcademicYear!.Value, request.YearNumber!.Value, request.StudyPlanMigrationReason),
                    command.ActorId, transactionCt);
            }
            return student;
        }, ct);

        created.User = user;
        created.Career = career;
        return await StudentDtoMapper.MapAsync(created, studentCareerRepository, studentAcademicRepository, ct);
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
