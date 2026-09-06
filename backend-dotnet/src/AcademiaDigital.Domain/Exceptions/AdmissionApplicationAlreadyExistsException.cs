namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionApplicationAlreadyExistsException()
    : Exception("Ya existe una solicitud de admisión para este formulario, email o DNI.");
