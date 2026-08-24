namespace AcademiaDigital.Domain.Exceptions;

public sealed class TeacherAlreadyExistsException(string field)
    : Exception($"A teacher is already registered with this {field}.");
