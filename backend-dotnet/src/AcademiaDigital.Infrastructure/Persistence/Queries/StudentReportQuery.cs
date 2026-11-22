using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Queries;

public sealed class StudentReportQuery(AppDbContext db) : IStudentReportQuery
{
    private const int MaxRows = 20000;

    public async Task<IReadOnlyList<StudentReportRow>> GetAsync(StudentReportFilter filter, CancellationToken ct = default)
    {
        var students = db.Students.AsNoTracking().Where(s => s.User.Role == UserRole.Alumno);
        students = filter.Kind == StudentReportKind.Applicants
            ? students.Where(s => s.User.RegistrationStatus == RegistrationStatus.Pending)
            : students.Where(s => s.User.RegistrationStatus == RegistrationStatus.Approved);
        if (filter.Kind == StudentReportKind.Students && filter.Status.HasValue)
            students = students.Where(s => s.Status == filter.Status.Value);
        if (filter.CareerId.HasValue)
            students = students.Where(s => s.CareerId == filter.CareerId.Value);

        // Rango de fechas: prematrícula (aspirantes), egreso (egresados) o inscripción (el resto).
        var from = filter.From?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = filter.To?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (filter.Kind == StudentReportKind.Applicants)
        {
            if (from.HasValue) students = students.Where(s => s.User.DateJoined >= from.Value);
            if (to.HasValue) students = students.Where(s => s.User.DateJoined < to.Value);
        }
        else if (filter.Status == StudentStatus.Graduated)
        {
            if (filter.From.HasValue) students = students.Where(s => s.GraduatedOn >= filter.From.Value);
            if (filter.To.HasValue) students = students.Where(s => s.GraduatedOn <= filter.To.Value);
        }
        else
        {
            if (from.HasValue) students = students.Where(s => s.EnrollmentDate >= from.Value);
            if (to.HasValue) students = students.Where(s => s.EnrollmentDate < to.Value);
        }

        var rows = await students
            .OrderBy(s => s.User.LastName).ThenBy(s => s.User.Username)
            .Take(MaxRows)
            .Select(s => new
            {
                s.LegajoNumber, s.User.LastName, s.User.Username, s.User.Dni, s.User.Cuil, s.User.Gender, s.User.BirthDate,
                s.User.Email, s.User.PhoneCode, s.User.Phone, s.User.AddressLine, s.User.City, s.User.Province, s.User.PostalCode,
                s.User.DocumentType, s.User.AlternatePhone, s.User.Neighborhood, s.User.District, s.User.Country,
                CareerName = s.Career.Name, s.Status, s.User.DateJoined, s.User.RegistrationStatus, s.EnrollmentDate, s.GraduatedOn,
                s.User.EmergencyContactName, s.User.EmergencyContactPhone, s.User.HealthInsurance, s.RegistryBook, s.RegistryFolio
            })
            .ToListAsync(ct);

        return rows.Select(r => new StudentReportRow(
            r.RegistrationStatus == RegistrationStatus.Pending ? null : r.LegajoNumber,
            r.LastName, r.Username, r.Dni, r.Cuil, r.Gender,
            r.BirthDate.HasValue ? DateOnly.FromDateTime(r.BirthDate.Value) : null,
            r.Email,
            string.IsNullOrEmpty(r.Phone) ? null : $"{r.PhoneCode} {r.Phone}".Trim(),
            r.AddressLine, r.City, r.Province, r.PostalCode, r.CareerName,
            r.RegistrationStatus == RegistrationStatus.Pending ? "Aspirante" : StatusLabel(r.Status),
            DateOnly.FromDateTime(r.DateJoined),
            r.RegistrationStatus == RegistrationStatus.Pending ? null : DateOnly.FromDateTime(r.EnrollmentDate),
            r.GraduatedOn, r.EmergencyContactName, r.EmergencyContactPhone, r.HealthInsurance,
            r.RegistryBook, r.RegistryFolio,
            r.DocumentType ?? (r.Dni is null ? null : "DNI"), r.AlternatePhone, r.Neighborhood, r.District, r.Country)).ToArray();
    }

    private static string StatusLabel(StudentStatus status) => status switch
    {
        StudentStatus.Regular => "Regular",
        StudentStatus.Libre => "Libre",
        StudentStatus.Graduated => "Egresado",
        StudentStatus.Withdrawn => "Baja",
        _ => status.ToString()
    };
}
