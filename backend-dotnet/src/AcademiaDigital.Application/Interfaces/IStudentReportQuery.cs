using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Application.Interfaces;

public enum StudentReportKind
{
    /// <summary>Alumnos dados de alta (con su estado: regular, libre, egresado o baja).</summary>
    Students,
    /// <summary>Aspirantes que se registraron y esperan el alta (prematriculados).</summary>
    Applicants
}

public sealed record StudentReportFilter(
    StudentReportKind Kind,
    StudentStatus? Status,
    int? CareerId,
    DateOnly? From,
    DateOnly? To);

/// <summary>Fila del listado para Excel: todos los datos que Secretaría usa para sus registros.</summary>
public sealed record StudentReportRow(
    string? LegajoNumber,
    string LastName,
    string FirstName,
    string? Dni,
    string? Cuil,
    string? Gender,
    DateOnly? BirthDate,
    string Email,
    string? Phone,
    string? AddressLine,
    string? City,
    string? Province,
    string? PostalCode,
    string? CareerName,
    string Status,
    DateOnly RegisteredOn,
    DateOnly? EnrolledOn,
    DateOnly? GraduatedOn,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? HealthInsurance,
    string? RegistryBook,
    string? RegistryFolio);

/// <summary>Listados de alumnos y aspirantes para exportar a Excel.</summary>
public interface IStudentReportQuery
{
    Task<IReadOnlyList<StudentReportRow>> GetAsync(StudentReportFilter filter, CancellationToken ct = default);
}
