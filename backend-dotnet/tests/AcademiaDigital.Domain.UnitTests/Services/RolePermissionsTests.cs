using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class RolePermissionsTests
{
    [Fact]
    public void Admin_has_every_permission()
        => Assert.All(Enum.GetValues<Permission>(), p => Assert.True(RolePermissions.Has(UserRole.Admin, p)));

    [Fact]
    public void Only_admin_manages_users_and_careers()
    {
        foreach (var role in new[] { UserRole.Secretario, UserRole.Coordinador, UserRole.Preceptor, UserRole.Cooperadora, UserRole.Profesor, UserRole.Alumno })
        {
            Assert.False(RolePermissions.Has(role, Permission.ManageUsers));
            Assert.False(RolePermissions.Has(role, Permission.ManageCareers));
        }
    }

    [Fact]
    public void Each_staff_role_gets_its_area()
    {
        Assert.True(RolePermissions.Has(UserRole.Secretario, Permission.ManageStudents));
        Assert.True(RolePermissions.Has(UserRole.Coordinador, Permission.ManageGrades));
        Assert.False(RolePermissions.Has(UserRole.Coordinador, Permission.ManageStudents));
        Assert.True(RolePermissions.Has(UserRole.Preceptor, Permission.ManageAttendance));
        Assert.False(RolePermissions.Has(UserRole.Preceptor, Permission.ManageGrades));
        Assert.True(RolePermissions.Has(UserRole.Cooperadora, Permission.ViewReports));
        Assert.False(RolePermissions.Has(UserRole.Cooperadora, Permission.ManageAttendance));
    }

    [Fact]
    public void Teachers_and_students_are_not_staff()
    {
        Assert.False(RolePermissions.IsStaff(UserRole.Profesor));
        Assert.False(RolePermissions.IsStaff(UserRole.Alumno));
        Assert.True(RolePermissions.IsStaff(UserRole.Preceptor));
    }
}
