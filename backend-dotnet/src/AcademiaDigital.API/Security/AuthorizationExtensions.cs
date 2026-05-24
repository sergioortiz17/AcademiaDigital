using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AcademiaDigital.API.Security;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.CanManageUsers, policy =>
                policy.RequireRole(UserRole.Administrador.ToString()));

            options.AddPolicy(AppPolicies.CanManageAcademicStructure, policy =>
                policy.RequireRole(
                    UserRole.Administrador.ToString(),
                    UserRole.Coordinador.ToString()));

            options.AddPolicy(AppPolicies.CanManageStudents, policy =>
                policy.RequireRole(
                    UserRole.Administrador.ToString(),
                    UserRole.Secretaria.ToString()));

            options.AddPolicy(AppPolicies.CanManageEnrollments, policy =>
                policy.RequireRole(
                    UserRole.Administrador.ToString(),
                    UserRole.Secretaria.ToString(),
                    UserRole.Coordinador.ToString()));

            options.AddPolicy(AppPolicies.CanLoadAttendance, policy =>
                policy.RequireRole(
                    UserRole.Docente.ToString(),
                    UserRole.Preceptor.ToString()));

            options.AddPolicy(AppPolicies.CanManageGrades, policy =>
                policy.RequireRole(
                    UserRole.Docente.ToString(),
                    UserRole.Secretaria.ToString(),
                    UserRole.Administrador.ToString()));

            options.AddPolicy(AppPolicies.CanManageTreasury, policy =>
                policy.RequireRole(
                    UserRole.TesoreriaCooperadora.ToString(),
                    UserRole.Administrador.ToString()));

            options.AddPolicy(AppPolicies.CanReadReports, policy =>
                policy.RequireRole(
                    UserRole.Administrador.ToString(),
                    UserRole.Secretaria.ToString(),
                    UserRole.Coordinador.ToString(),
                    UserRole.TesoreriaCooperadora.ToString()));

            options.AddPolicy(AppPolicies.CanReadAuditLogs, policy =>
                policy.RequireRole(UserRole.Administrador.ToString()));
        });

        return services;
    }
}
