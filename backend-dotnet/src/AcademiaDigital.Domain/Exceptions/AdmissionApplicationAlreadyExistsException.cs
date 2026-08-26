namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionApplicationAlreadyExistsException()
    : Exception("An admission application already exists for this form, email or DNI.");
