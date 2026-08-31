using AcademiaDigital.Application.UseCases.StudyPlanDiff;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

[ApiController]
[Route("api/v1/study-plans")]
public class StudyPlanDiffController(GetStudyPlanDiffQueryHandler diffHandler) : ControllerBase
{
    [HttpGet("{planAId:int}/diff/{planBId:int}")]
    public async Task<IActionResult> Diff(int planAId, int planBId, CancellationToken ct)
    {
        try
        {
            var result = await diffHandler.Handle(new GetStudyPlanDiffQuery(planAId, planBId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
