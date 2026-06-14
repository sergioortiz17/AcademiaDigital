using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Students;

public sealed record GetStudentsQuery(int? CareerId);
public sealed record GetStudentByIdQuery(long StudentId);
public sealed record CreateStudentCommand(CreateStudentRequest Request);

public sealed class GetStudentsQueryHandler(
    IStudentRepository studentRepository,
    IStudentAcademicRepository studentAcademicRepository)
{
    public async Task<IReadOnlyList<StudentDto>> Handle(GetStudentsQuery query, CancellationToken ct = default)
    {
        var students = query.CareerId.HasValue
            ? await studentRepository.GetByCareerAsync(query.CareerId.Value, ct)
            : await studentRepository.GetAllAsync(ct);

        var studentList = students.ToList();
        var currentStudyPlans = await studentAcademicRepository.GetCurrentStudyPlansAsync(studentList.Select(s => s.Id), ct);

        return studentList
            .Select(student =>
            {
                currentStudyPlans.TryGetValue(student.Id, out var currentStudyPlan);
                return Map(student, currentStudyPlan);
            })
            .ToList();
    }

    private static StudentDto Map(Student student, StudentStudyPlan? currentStudyPlan) => new()
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
        CurrentStudyPlanId = currentStudyPlan?.StudyPlanId,
        CurrentStudyPlanName = currentStudyPlan?.StudyPlan.Name
    };
}

public sealed class GetStudentByIdQueryHandler(
    IStudentRepository studentRepository,
    IStudentAcademicRepository studentAcademicRepository)
{
    public async Task<StudentDto> Handle(GetStudentByIdQuery query, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByIdAsync(query.StudentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        var currentStudyPlan = await studentAcademicRepository.GetCurrentStudyPlanAsync(student.Id, ct);
        return Map(student, currentStudyPlan);
    }

    private static StudentDto Map(Student student, StudentStudyPlan? currentStudyPlan) => new()
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
        CurrentStudyPlanId = currentStudyPlan?.StudyPlanId,
        CurrentStudyPlanName = currentStudyPlan?.StudyPlan.Name
    };
}

public sealed class CreateStudentCommandHandler(
    IUserRepository userRepository,
    ICareerRepository careerRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudentRepository studentRepository,
    IStudentAcademicRepository studentAcademicRepository)
{
    public async Task<StudentDto> Handle(CreateStudentCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var user = await userRepository.FindByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role != UserRole.Alumno)
            throw new InvalidOperationException("Only users with Alumno role can be linked as students.");

        var career = await careerRepository.FindByIdAsync(request.CareerId, ct)
            ?? throw new KeyNotFoundException("Career not found.");

        var existingByUser = await studentRepository.FindByUserIdAsync(user.Id, ct);
        if (existingByUser is not null)
            throw new InvalidOperationException("User is already linked to a student.");

        var legajo = request.LegajoNumber.Trim();
        var existingByLegajo = await studentRepository.FindByLegajoAsync(legajo, ct);
        if (existingByLegajo is not null)
            throw new InvalidOperationException("Legajo number already exists.");

        StudyPlan? studyPlan = null;
        if (request.StudyPlanId.HasValue)
        {
            studyPlan = await studyPlanRepository.GetByIdAsync(request.StudyPlanId.Value, ct)
                ?? throw new KeyNotFoundException("Study plan not found.");

            if (studyPlan.CareerId != career.Id)
                throw new InvalidOperationException("Study plan must belong to the selected career.");
        }

        var status = Enum.Parse<StudentStatus>(request.Status, ignoreCase: true);
        var student = new Student
        {
            UserId = user.Id,
            CareerId = career.Id,
            LegajoNumber = legajo,
            EnrollmentDate = request.EnrollmentDate ?? DateTime.UtcNow,
            Status = status
        };

        var created = await studentRepository.CreateAsync(student, ct);

        StudentStudyPlan? currentStudyPlan = null;
        if (studyPlan is not null)
        {
            currentStudyPlan = await studentAcademicRepository.AssignStudyPlanAsync(new StudentStudyPlan
            {
                StudentId = created.Id,
                StudyPlanId = studyPlan.Id,
                MigrationReason = request.StudyPlanMigrationReason
            }, ct);

            currentStudyPlan.StudyPlan = studyPlan;
        }

        created.User = user;
        created.Career = career;
        return Map(created, currentStudyPlan);
    }

    private static StudentDto Map(Student student, StudentStudyPlan? currentStudyPlan) => new()
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
        CurrentStudyPlanId = currentStudyPlan?.StudyPlanId,
        CurrentStudyPlanName = currentStudyPlan?.StudyPlan.Name
    };
}
