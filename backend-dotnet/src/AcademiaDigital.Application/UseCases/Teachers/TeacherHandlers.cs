using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Teachers;

public sealed record GetTeachersQuery(bool IncludeInactive = false);
public sealed record GetTeacherByIdQuery(long TeacherId);

public sealed record CreateTeacherCommand(
    long UserId,
    string EmployeeNumber,
    string? Department,
    string? SpecializationArea,
    DateTime HireDate,
    string? PhoneNumber,
    string? AddressLine,
    string? City,
    string? Province,
    string? PostalCode,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone);

public sealed record UpdateTeacherCommand(
    long TeacherId,
    string EmployeeNumber,
    string? Department,
    string? SpecializationArea,
    DateTime HireDate,
    string? PhoneNumber,
    string? AddressLine,
    string? City,
    string? Province,
    string? PostalCode,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone);

public sealed record DeactivateTeacherCommand(long TeacherId, long ActorUserId, string? Reason);

public sealed record TeacherDto(
    long Id,
    long UserId,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string? Dni,
    string? Gender,
    DateTime? BirthDate,
    string? Department,
    string? SpecializationArea,
    DateTime HireDate,
    bool IsActive,
    string? PhoneNumber,
    string? AddressLine,
    string? City,
    string? Province,
    string? PostalCode,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    DateTime? DeactivatedAt,
    long? DeactivatedByUserId,
    string? DeactivationReason);

public sealed class GetTeachersQueryHandler(ITeacherRepository repository)
{
    public async Task<IReadOnlyList<TeacherDto>> Handle(GetTeachersQuery query, CancellationToken ct = default)
        => (await repository.GetAllAsync(query.IncludeInactive, ct)).Select(TeacherDtoMapper.Map).ToList();
}

public sealed class GetTeacherByIdQueryHandler(ITeacherRepository repository)
{
    public async Task<TeacherDto> Handle(GetTeacherByIdQuery query, CancellationToken ct = default)
        => TeacherDtoMapper.Map(await repository.FindByIdAsync(query.TeacherId, ct)
            ?? throw new KeyNotFoundException("Teacher not found."));
}

public sealed class CreateTeacherCommandHandler(
    IUserRepository userRepository,
    ITeacherRepository teacherRepository,
    TeacherProfilePolicy policy,
    TimeProvider timeProvider)
{
    public async Task<TeacherDto> Handle(CreateTeacherCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        if (user.Role != UserRole.Profesor)
            throw new InvalidOperationException("Only users with Profesor role can be linked as teachers.");
        if (!user.IsActive)
            throw new InvalidOperationException("An inactive user cannot be linked as a teacher.");
        if (await teacherRepository.FindByUserIdAsync(user.Id, ct) is not null)
            throw new TeacherAlreadyExistsException("user");

        var employeeNumber = policy.NormalizeEmployeeNumber(command.EmployeeNumber);
        policy.ValidateHireDate(command.HireDate, timeProvider.GetUtcNow().UtcDateTime);
        if (await teacherRepository.FindByEmployeeNumberAsync(employeeNumber, ct) is not null)
            throw new TeacherAlreadyExistsException("employee number");

        var teacher = ApplyProfile(new Teacher
        {
            UserId = user.Id,
            IsActive = true
        }, command.EmployeeNumber, command.Department, command.SpecializationArea, command.HireDate,
            command.PhoneNumber, command.AddressLine, command.City, command.Province, command.PostalCode,
            command.EmergencyContactName, command.EmergencyContactRelationship, command.EmergencyContactPhone,
            employeeNumber);

        var created = await teacherRepository.CreateAsync(teacher, ct);
        created.User = user;
        return TeacherDtoMapper.Map(created);
    }

    internal static Teacher ApplyProfile(
        Teacher teacher,
        string employeeNumber,
        string? department,
        string? specializationArea,
        DateTime hireDate,
        string? phoneNumber,
        string? addressLine,
        string? city,
        string? province,
        string? postalCode,
        string? emergencyContactName,
        string? emergencyContactRelationship,
        string? emergencyContactPhone,
        string? normalizedEmployeeNumber = null)
    {
        teacher.EmployeeNumber = normalizedEmployeeNumber ?? employeeNumber.Trim().ToUpperInvariant();
        teacher.Department = Clean(department);
        teacher.SpecializationArea = Clean(specializationArea);
        teacher.HireDate = hireDate.Date;
        teacher.PhoneNumber = Clean(phoneNumber);
        teacher.AddressLine = Clean(addressLine);
        teacher.City = Clean(city);
        teacher.Province = Clean(province);
        teacher.PostalCode = Clean(postalCode);
        teacher.EmergencyContactName = Clean(emergencyContactName);
        teacher.EmergencyContactRelationship = Clean(emergencyContactRelationship);
        teacher.EmergencyContactPhone = Clean(emergencyContactPhone);
        return teacher;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdateTeacherCommandHandler(
    ITeacherRepository repository,
    TeacherProfilePolicy policy,
    TimeProvider timeProvider)
{
    public async Task<TeacherDto> Handle(UpdateTeacherCommand command, CancellationToken ct = default)
    {
        var teacher = await repository.FindByIdAsync(command.TeacherId, ct)
            ?? throw new KeyNotFoundException("Teacher not found.");
        var employeeNumber = policy.NormalizeEmployeeNumber(command.EmployeeNumber);
        policy.ValidateHireDate(command.HireDate, timeProvider.GetUtcNow().UtcDateTime);
        var duplicate = await repository.FindByEmployeeNumberAsync(employeeNumber, ct);
        if (duplicate is not null && duplicate.Id != teacher.Id)
            throw new TeacherAlreadyExistsException("employee number");

        CreateTeacherCommandHandler.ApplyProfile(
            teacher, command.EmployeeNumber, command.Department, command.SpecializationArea, command.HireDate,
            command.PhoneNumber, command.AddressLine, command.City, command.Province, command.PostalCode,
            command.EmergencyContactName, command.EmergencyContactRelationship, command.EmergencyContactPhone,
            employeeNumber);
        var updated = await repository.UpdateAsync(teacher, ct);
        return TeacherDtoMapper.Map(updated);
    }
}

public sealed class DeactivateTeacherCommandHandler(
    ITeacherRepository repository,
    TeacherProfilePolicy policy,
    TimeProvider timeProvider)
{
    public async Task Handle(DeactivateTeacherCommand command, CancellationToken ct = default)
    {
        var teacher = await repository.FindByIdAsync(command.TeacherId, ct)
            ?? throw new KeyNotFoundException("Teacher not found.");
        if (!teacher.IsActive)
            return;

        policy.Deactivate(teacher, command.ActorUserId, command.Reason, timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpdateAsync(teacher, ct);
    }
}

internal static class TeacherDtoMapper
{
    public static TeacherDto Map(Teacher teacher) => new(
        teacher.Id,
        teacher.UserId,
        teacher.EmployeeNumber,
        teacher.User.Username,
        teacher.User.LastName,
        teacher.User.Email,
        teacher.User.Dni,
        teacher.User.Gender,
        teacher.User.BirthDate,
        teacher.Department,
        teacher.SpecializationArea,
        teacher.HireDate,
        teacher.IsActive,
        teacher.PhoneNumber,
        teacher.AddressLine,
        teacher.City,
        teacher.Province,
        teacher.PostalCode,
        teacher.EmergencyContactName,
        teacher.EmergencyContactRelationship,
        teacher.EmergencyContactPhone,
        teacher.DeactivatedAt,
        teacher.DeactivatedByUserId,
        teacher.DeactivationReason);
}
