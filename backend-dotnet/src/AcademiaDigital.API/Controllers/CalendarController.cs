using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.API.Controllers;

[Route("api/v1/calendar")]
public class CalendarController(AppDbContext db) : ApiControllerBase
{
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (year < 2020 || year > 2100 || month < 1 || month > 12)
            return BadRequest(new { success = false, msg = "Año o mes inválido." });

        var events = await db.AcademicEvents
            .AsNoTracking()
            .Where(e => e.IsPublished && e.EventDate.Year == year && e.EventDate.Month == month)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.EventType,
                Date = e.EventDate.ToString("yyyy-MM-dd"),
                StartTime = e.StartTime.HasValue ? e.StartTime.Value.ToString("HH:mm") : null
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = events });
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest req, CancellationToken ct)
    {
        if (CurrentUserRole is not Domain.Enums.UserRole.Admin)
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { success = false, msg = "El título es requerido." });

        var ev = new Domain.Entities.AcademicEvent
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            EventDate = DateOnly.Parse(req.Date),
            StartTime = req.StartTime != null ? TimeOnly.Parse(req.StartTime) : null,
            EventType = req.EventType ?? "Otro",
            IsPublished = true
        };

        db.AcademicEvents.Add(ev);
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, data = new { ev.Id, ev.Title, ev.EventType, Date = ev.EventDate.ToString("yyyy-MM-dd") } });
    }

    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        if (CurrentUserRole is not Domain.Enums.UserRole.Admin)
            return Forbid();

        var ev = await db.AcademicEvents.FindAsync([id], ct);
        if (ev is null) return NotFound(new { success = false, msg = "Evento no encontrado." });

        db.AcademicEvents.Remove(ev);
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }
}

public record CreateEventRequest(string Title, string? Description, string Date, string? StartTime, string? EventType);
