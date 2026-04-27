using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected long? CurrentUserId
        => HttpContext.Items.TryGetValue("UserId", out var id) && id is long uid ? uid : null;

    protected bool IsSuperuser
        => HttpContext.Items.TryGetValue("IsSuperuser", out var v) && v is bool b && b;
}
