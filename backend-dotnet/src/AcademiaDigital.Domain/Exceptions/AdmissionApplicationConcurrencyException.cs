namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionApplicationConcurrencyException()
    : Exception("La solicitud de admisión fue modificada por otra operación. Recargala y volvé a intentar.");
