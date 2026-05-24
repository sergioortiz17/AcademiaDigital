using AcademiaDigital.API.Models;
using AcademiaDigital.API.Security;
using AcademiaDigital.Application.UseCases.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[Authorize(Policy = AppPolicies.CanReadAuditLogs)]
[Route("api/v1/admin/audit-logs")]
public class AdminAuditLogsController(ListAdminAuditLogsUseCase listAdminAuditLogsUseCase) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAuditLogs(CancellationToken ct)
    {
        var auditLogs = await listAdminAuditLogsUseCase.ExecuteAsync(ct);
        return Ok(ApiResponse.Ok(auditLogs));
    }
}
