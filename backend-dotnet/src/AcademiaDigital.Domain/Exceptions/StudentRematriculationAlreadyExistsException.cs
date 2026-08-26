namespace AcademiaDigital.Domain.Exceptions;

public sealed class StudentRematriculationAlreadyExistsException(long studentCareerId, int academicYear)
    : Exception($"Student career {studentCareerId} already has a rematriculation for academic year {academicYear}.");
