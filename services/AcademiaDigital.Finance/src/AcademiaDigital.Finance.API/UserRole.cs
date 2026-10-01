namespace AcademiaDigital.Finance.API;

// Mirrors the monolito's UserRole so the Finance API can honour the caller identity that
// the monolito/gateway forwards (X-User-Id / X-User-Role headers). Finance does not own
// authentication; it trusts the identity headers set at the deployment boundary.
public enum UserRole
{
    Alumno = 1,
    Profesor = 2,
    Admin = 3
}
