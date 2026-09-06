using System.Globalization;
using System.Text;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Domain.Services;

public sealed class CertificatePolicy
{
    private static readonly IReadOnlyDictionary<string, CertificateKind> Aliases =
        new Dictionary<string, CertificateKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["regularstudent"] = CertificateKind.RegularStudent,
            ["alumnoregular"] = CertificateKind.RegularStudent,
            ["certificadodealumnoregular"] = CertificateKind.RegularStudent,
            ["enrollment"] = CertificateKind.Enrollment,
            ["matricula"] = CertificateKind.Enrollment,
            ["constanciadematricula"] = CertificateKind.Enrollment,
            ["constanciadeinscripcion"] = CertificateKind.Enrollment,
            ["approvedcourses"] = CertificateKind.ApprovedCourses,
            ["materiasaprobadas"] = CertificateKind.ApprovedCourses,
            ["certificadodemateriasaprobadas"] = CertificateKind.ApprovedCourses,
            ["academicstatus"] = CertificateKind.AcademicStatus,
            ["situacionacademica"] = CertificateKind.AcademicStatus,
            ["constanciadesituacionacademica"] = CertificateKind.AcademicStatus,
            ["certificadodepromedio"] = CertificateKind.AcademicStatus,
            ["transcript"] = CertificateKind.Transcript,
            ["analitico"] = CertificateKind.Transcript,
            ["certificadoanalitico"] = CertificateKind.Transcript,
            ["generalacademicstatus"] = CertificateKind.GeneralAcademicStatus,
            ["estadoacademicogeneral"] = CertificateKind.GeneralAcademicStatus,
            ["constanciadeegreso"] = CertificateKind.GeneralAcademicStatus,
            ["exampermit"] = CertificateKind.ExamPermit,
            ["permisodeexamen"] = CertificateKind.ExamPermit
        };

    public CertificateKind ParseKind(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("El tipo de certificado es obligatorio.");
        var key = Normalize(value);
        if (!Aliases.TryGetValue(key, out var kind))
            throw new ArgumentException("Tipo de certificado no soportado.");
        return kind;
    }

    public string DisplayName(CertificateKind kind) => kind switch
    {
        CertificateKind.RegularStudent => "Certificado de alumno regular",
        CertificateKind.Enrollment => "Constancia de matrícula",
        CertificateKind.ApprovedCourses => "Certificado de materias aprobadas",
        CertificateKind.AcademicStatus => "Constancia de situación académica",
        CertificateKind.Transcript => "Certificado analítico",
        CertificateKind.GeneralAcademicStatus => "Estado académico general",
        CertificateKind.ExamPermit => "Permiso de examen",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public void EnsureEligible(CertificateKind kind, CertificateAcademicRecord record, long? examRegistrationId)
    {
        if (!record.StudentCareerIsActive)
            throw new InvalidOperationException("La carrera del estudiante seleccionada no está activa.");
        if (string.IsNullOrWhiteSpace(record.Dni)
            || string.IsNullOrWhiteSpace(record.StudentName)
            || string.IsNullOrWhiteSpace(record.CareerName))
            throw new InvalidOperationException("El nombre, el DNI y la carrera del estudiante son obligatorios para emitir un certificado.");

        switch (kind)
        {
            case CertificateKind.RegularStudent when record.StudentStatus != StudentStatus.Regular:
                throw new InvalidOperationException("Un certificado de alumno regular requiere el estado regular.");
            case CertificateKind.Enrollment when !record.Courses.Any(course => course.Status != EnrollmentStatus.Withdrawn):
                throw new InvalidOperationException("Una constancia de matrícula requiere al menos una inscripción activa.");
            case CertificateKind.ApprovedCourses when !record.Courses.Any(course =>
                course.Status is EnrollmentStatus.Approved or EnrollmentStatus.Promoted):
                throw new InvalidOperationException("Un certificado de materias aprobadas requiere al menos una materia aprobada.");
            case CertificateKind.ExamPermit:
                if (!examRegistrationId.HasValue)
                    throw new ArgumentException("Se requiere el id de inscripción al examen para un permiso de examen.");
                if (record.Exam is null || record.Exam.RegistrationId != examRegistrationId.Value)
                    throw new InvalidOperationException("La inscripción al examen no pertenece a la carrera del estudiante seleccionada.");
                if (record.Exam.Status != ExamTableStatus.Open)
                    throw new InvalidOperationException("Los permisos de examen solo pueden emitirse mientras la mesa de examen está abierta.");
                break;
        }
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        return string.Concat(decomposed
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit))
            .ToLowerInvariant();
    }
}
