using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected long? CurrentUserId
        => HttpContext.Items.TryGetValue("UserId", out var id) && id is long uid ? uid : null;

    protected UserRole? CurrentUserRole
        => HttpContext.Items.TryGetValue("UserRole", out var r) && r is int role ? (UserRole)role : null;

    protected bool IsSuperuser
        => HttpContext.Items.TryGetValue("IsSuperuser", out var v) && v is bool b && b;

    /// <summary>Área de la gestión del controlador; sin área, solo el Admin entra como personal.</summary>
    protected virtual Permission? Area => null;

    /// <summary>Admin, o un rol de gestión (secretaría, coordinación, preceptoría, cooperadora) con permiso sobre el área.</summary>
    protected bool IsStaff => RolePermissions.Has(CurrentUserRole, Area ?? Permission.ManageUsers);

    protected bool Can(Permission permission) => RolePermissions.Has(CurrentUserRole, permission);
}
