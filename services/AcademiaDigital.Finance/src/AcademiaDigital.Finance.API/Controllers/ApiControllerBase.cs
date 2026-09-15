using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.Finance.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // Identity is forwarded by the monolito/gateway as headers. Finance trusts them at the
    // deployment boundary (internal service); it never re-authenticates.
    protected long? CurrentUserId
        => long.TryParse(Request.Headers["X-User-Id"].FirstOrDefault(), out var id) ? id : null;

    protected UserRole? CurrentUserRole
        => Enum.TryParse<UserRole>(Request.Headers["X-User-Role"].FirstOrDefault(), true, out var role) ? role : null;
}
