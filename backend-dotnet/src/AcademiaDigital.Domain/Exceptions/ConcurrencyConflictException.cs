namespace AcademiaDigital.Domain.Exceptions;

public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : Exception(message, innerException);
