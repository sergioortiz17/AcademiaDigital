namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionApplicationConcurrencyException()
    : Exception("The admission application was modified by another operation. Reload it and retry.");
