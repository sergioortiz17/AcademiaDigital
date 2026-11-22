namespace AcademiaDigital.Domain.Enums;

/// <summary>Áreas de la gestión que se pueden habilitar por rol (además del Admin, que tiene todas).</summary>
public enum Permission
{
    /// <summary>Usuarios y roles.</summary>
    ManageUsers,
    /// <summary>Carreras, planes, reglas de aprobación, divisiones y comisiones.</summary>
    ManageCareers,
    /// <summary>Consultar alumnos y sus fichas.</summary>
    ViewStudents,
    /// <summary>Alumnos, aspirantes, requisitos de ingreso, analíticos y acciones masivas.</summary>
    ManageStudents,
    /// <summary>Legajos docentes y asignación de materias.</summary>
    ManageTeachers,
    /// <summary>Todas las planillas de calificaciones (aprobar, publicar, cerrar).</summary>
    ManageGrades,
    /// <summary>Asistencia de todas las comisiones (justificar, reabrir).</summary>
    ManageAttendance,
    /// <summary>Mesas de examen.</summary>
    ManageExams,
    /// <summary>Períodos e inscripciones a materias.</summary>
    ManageEnrollments,
    /// <summary>Certificados y constancias.</summary>
    ManageCertificates,
    /// <summary>Eventos institucionales del calendario.</summary>
    ManageCalendar,
    /// <summary>Reportes y exportaciones a Excel.</summary>
    ViewReports
}
