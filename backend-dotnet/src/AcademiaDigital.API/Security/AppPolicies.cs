namespace AcademiaDigital.API.Security;

public static class AppPolicies
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanManageAcademicStructure = nameof(CanManageAcademicStructure);
    public const string CanManageStudents = nameof(CanManageStudents);
    public const string CanManageEnrollments = nameof(CanManageEnrollments);
    public const string CanLoadAttendance = nameof(CanLoadAttendance);
    public const string CanManageGrades = nameof(CanManageGrades);
    public const string CanManageTreasury = nameof(CanManageTreasury);
    public const string CanReadReports = nameof(CanReadReports);
    public const string CanReadAuditLogs = nameof(CanReadAuditLogs);
}
