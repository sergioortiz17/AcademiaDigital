namespace AcademiaDigital.Domain.Exceptions;

public sealed class TeacherAlreadyExistsException(string field)
    : Exception($"Ya existe un docente registrado con este {field}.");
