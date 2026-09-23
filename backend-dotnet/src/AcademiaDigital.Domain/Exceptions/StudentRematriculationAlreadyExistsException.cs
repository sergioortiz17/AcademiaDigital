namespace AcademiaDigital.Domain.Exceptions;

public sealed class StudentRematriculationAlreadyExistsException(long studentCareerId, int academicYear)
    : Exception($"La carrera del alumno {studentCareerId} ya tiene una rematriculación para el año académico {academicYear}.");
