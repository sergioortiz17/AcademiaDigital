using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Services;

/// <summary>
/// Qué puede hacer cada rol de gestión. Admin tiene todo. Propuesta inicial para validar con
/// Dirección (reunión con Secretaría): se ajusta acá y vale para el backend y el menú.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<UserRole, IReadOnlySet<Permission>> Matrix =
        new Dictionary<UserRole, IReadOnlySet<Permission>>
        {
            [UserRole.Secretario] = new HashSet<Permission>
            {
                Permission.ViewStudents, Permission.ManageStudents, Permission.ManageTeachers, Permission.ManageGrades,
                Permission.ManageAttendance, Permission.ManageExams, Permission.ManageEnrollments,
                Permission.ManageCertificates, Permission.ManageCalendar, Permission.ViewReports
            },
            [UserRole.Coordinador] = new HashSet<Permission>
            {
                Permission.ViewStudents, Permission.ManageGrades, Permission.ManageAttendance, Permission.ManageExams,
                Permission.ManageCalendar, Permission.ViewReports
            },
            [UserRole.Preceptor] = new HashSet<Permission>
            {
                Permission.ViewStudents, Permission.ManageAttendance, Permission.ManageCalendar
            },
            [UserRole.Cooperadora] = new HashSet<Permission>
            {
                Permission.ViewStudents, Permission.ViewReports
            }
        };

    /// <summary>Roles de gestión (personal de la institución que no es docente ni alumno).</summary>
    public static bool IsStaff(UserRole? role) => role is UserRole.Admin || (role.HasValue && Matrix.ContainsKey(role.Value));

    public static bool Has(UserRole? role, Permission permission)
        => role == UserRole.Admin || (role.HasValue && Matrix.TryGetValue(role.Value, out var set) && set.Contains(permission));

    public static IReadOnlyCollection<Permission> For(UserRole? role)
        => role == UserRole.Admin ? Enum.GetValues<Permission>()
            : role.HasValue && Matrix.TryGetValue(role.Value, out var set) ? set.ToArray() : [];
}
