namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionFormSlugAlreadyExistsException(string slug)
    : Exception($"Ya existe un formulario de admisión con el slug '{slug}'.");
