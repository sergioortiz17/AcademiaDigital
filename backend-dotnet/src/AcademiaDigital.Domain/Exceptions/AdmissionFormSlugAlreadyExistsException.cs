namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionFormSlugAlreadyExistsException(string slug)
    : Exception($"An admission form with slug '{slug}' already exists.");
