namespace AcademiaDigital.Domain.Enums;

public enum UserRole
{
    Alumno = 1,
    Profesor = 2,
    Admin = 3,
    /// <summary>Secretaría / jefe de personal: toda la gestión administrativa, sin usuarios ni planes.</summary>
    Secretario = 4,
    /// <summary>Coordinación académica: planillas, mesas, asistencia y reportes.</summary>
    Coordinador = 5,
    /// <summary>Preceptoría y ayudantes técnicos: asistencia y consulta de alumnos.</summary>
    Preceptor = 6,
    /// <summary>Cooperadora / caja: consulta de alumnos y reportes para cobranzas.</summary>
    Cooperadora = 7
}
