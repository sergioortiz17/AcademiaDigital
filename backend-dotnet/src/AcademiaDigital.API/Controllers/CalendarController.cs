using AcademiaDigital.API.Helpers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
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

        var userId = CurrentUserId;
        var role = CurrentUserRole;

        IQueryable<AcademicEvent> query = db.AcademicEvents
            .AsNoTracking()
            .Where(e => e.IsPublished && e.EventDate.Year == year && e.EventDate.Month == month);

        query = await ApplyVisibilityFilter(query, userId, role, ct);

        var raw = await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                EventType = e.EventType,
                EventDate = e.EventDate,
                StartTime = e.StartTime,
                Scope = e.Scope,
                Modality = e.Modality,
                CourseSectionId = e.CourseSectionId,
                CourseName = e.CourseSection != null ? e.CourseSection.Course.Name : null,
                CreatedByName = e.CreatedByUser.Username + " " + e.CreatedByUser.LastName,
                CreatedByUserId = e.CreatedByUserId
            })
            .ToListAsync(ct);

        var events = raw.Select(MapToResponse).ToList();

        var holidays = ArgentineHolidays.GetForMonth(year, month);
        foreach (var (date, name) in holidays)
        {
            events.Add(new EventResponse
            {
                Id = 0,
                Title = name,
                Description = "Feriado nacional",
                EventType = "Feriado",
                Date = date.ToString("yyyy-MM-dd"),
                StartTime = null,
                Scope = "Global",
                Modality = null,
                CourseSectionId = null,
                CourseName = null,
                CreatedByName = null,
                CreatedByUserId = 0,
                IsHoliday = true
            });
        }

        var sorted = events.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
        return Ok(new { success = true, data = sorted });
    }

    [HttpGet("my-sections")]
    public async Task<IActionResult> GetMySections(CancellationToken ct)
    {
        if (CurrentUserRole is not UserRole.Profesor || CurrentUserId is not { } uid)
            return Forbid();

        var teacherId = await db.Teachers
            .Where(t => t.UserId == uid)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (teacherId == 0)
            return Ok(new { success = true, data = Array.Empty<object>() });

        var sections = await db.TeacherAssignments
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId && a.IsCurrent)
            .Select(a => new
            {
                a.CourseSection.Id,
                CourseName = a.CourseSection.Course.Name,
                CourseCode = a.CourseSection.Course.Code,
                a.CourseSection.AcademicYear,
                a.CourseSection.Semester
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = sections });
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest req, CancellationToken ct)
    {
        var uid = CurrentUserId;
        var role = CurrentUserRole;

        if (uid is null) return Unauthorized();
        if (role is not (UserRole.Admin or UserRole.Profesor))
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { success = false, msg = "El título es requerido." });

        var scope = EventScope.Global;
        int? courseSectionId = null;

        if (role == UserRole.Profesor)
        {
            if (req.CourseSectionId is null)
                return BadRequest(new { success = false, msg = "Debe seleccionar una comisión." });

            var teacherId = await db.Teachers
                .Where(t => t.UserId == uid.Value)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(ct);

            var hasAssignment = await db.TeacherAssignments
                .AnyAsync(a => a.TeacherId == teacherId
                    && a.CourseSectionId == req.CourseSectionId.Value
                    && a.IsCurrent, ct);

            if (!hasAssignment)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, msg = "No tiene asignación activa en esa comisión." });

            scope = EventScope.CourseSection;
            courseSectionId = req.CourseSectionId.Value;
        }
        else if (role == UserRole.Admin && req.CourseSectionId is not null)
        {
            scope = EventScope.CourseSection;
            courseSectionId = req.CourseSectionId.Value;
        }

        string? modality = null;
        if (role == UserRole.Admin && req.Modality is "Presencial" or "Virtual")
            modality = req.Modality;

        var ev = new AcademicEvent
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            EventDate = DateOnly.Parse(req.Date),
            StartTime = req.StartTime != null ? TimeOnly.Parse(req.StartTime) : null,
            EventType = req.EventType ?? "Otro",
            Scope = scope,
            Modality = modality,
            CourseSectionId = courseSectionId,
            CreatedByUserId = uid.Value,
            IsPublished = true
        };

        db.AcademicEvents.Add(ev);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            data = new
            {
                ev.Id,
                ev.Title,
                ev.EventType,
                Date = ev.EventDate.ToString("yyyy-MM-dd"),
                Scope = ev.Scope.ToString(),
                ev.Modality,
                ev.CourseSectionId
            }
        });
    }

    [HttpPut("events/{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventRequest req, CancellationToken ct)
    {
        var uid = CurrentUserId;
        var role = CurrentUserRole;
        if (uid is null) return Unauthorized();

        var ev = await db.AcademicEvents.FindAsync([id], ct);
        if (ev is null) return NotFound(new { success = false, msg = "Evento no encontrado." });

        if (role == UserRole.Profesor && ev.CreatedByUserId != uid.Value)
            return Forbid();
        if (role is not (UserRole.Admin or UserRole.Profesor))
            return Forbid();

        if (!string.IsNullOrWhiteSpace(req.Title)) ev.Title = req.Title.Trim();
        ev.Description = req.Description?.Trim();
        ev.EventDate = DateOnly.Parse(req.Date);
        ev.StartTime = req.StartTime != null ? TimeOnly.Parse(req.StartTime) : null;
        ev.EventType = req.EventType ?? ev.EventType;

        if (role == UserRole.Admin)
            ev.Modality = req.Modality is "Presencial" or "Virtual" ? req.Modality : null;

        await db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        var uid = CurrentUserId;
        var role = CurrentUserRole;
        if (uid is null) return Unauthorized();

        var ev = await db.AcademicEvents.FindAsync([id], ct);
        if (ev is null) return NotFound(new { success = false, msg = "Evento no encontrado." });

        if (role == UserRole.Profesor && ev.CreatedByUserId != uid.Value)
            return Forbid();
        if (role is not (UserRole.Admin or UserRole.Profesor))
            return Forbid();

        db.AcademicEvents.Remove(ev);
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming(CancellationToken ct)
    {
        var uid = CurrentUserId;
        var role = CurrentUserRole;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<AcademicEvent> query = db.AcademicEvents
            .AsNoTracking()
            .Where(e => e.IsPublished && e.EventDate >= today);

        query = await ApplyVisibilityFilter(query, uid, role, ct);

        var raw = await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .Take(10)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                EventType = e.EventType,
                EventDate = e.EventDate,
                StartTime = e.StartTime,
                Scope = e.Scope,
                Modality = e.Modality,
                CourseName = e.CourseSection != null ? e.CourseSection.Course.Name : null
            })
            .ToListAsync(ct);

        var events = raw.Select(MapToResponse).ToList();

        var upcomingHolidays = ArgentineHolidays.GetForYear(today.Year)
            .Where(h => h.Date >= today)
            .Take(3);

        foreach (var (date, name) in upcomingHolidays)
        {
            events.Add(new EventResponse
            {
                Id = 0,
                Title = name,
                Description = "Feriado nacional",
                EventType = "Feriado",
                Date = date.ToString("yyyy-MM-dd"),
                Scope = "Global",
                IsHoliday = true
            });
        }

        var sorted = events.OrderBy(e => e.Date).ThenBy(e => e.StartTime).Take(5).ToList();
        return Ok(new { success = true, data = sorted });
    }

    private static EventResponse MapToResponse(EventDto e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        EventType = e.EventType,
        Date = e.EventDate.ToString("yyyy-MM-dd"),
        StartTime = e.StartTime?.ToString("HH:mm"),
        Scope = e.Scope.ToString(),
        Modality = e.Modality,
        CourseSectionId = e.CourseSectionId,
        CourseName = e.CourseName,
        CreatedByName = e.CreatedByName,
        CreatedByUserId = e.CreatedByUserId,
        IsHoliday = false
    };

    private async Task<IQueryable<AcademicEvent>> ApplyVisibilityFilter(
        IQueryable<AcademicEvent> query, long? userId, UserRole? role, CancellationToken ct)
    {
        if (role == UserRole.Alumno && userId.HasValue)
        {
            var studentId = await db.Students
                .Where(s => s.UserId == userId.Value)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            var enrolledSectionIds = await db.Enrollments
                .Where(e => e.StudentId == studentId
                    && e.CourseSectionId.HasValue
                    && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Regularized))
                .Select(e => e.CourseSectionId!.Value)
                .Distinct()
                .ToListAsync(ct);

            query = query.Where(e =>
                e.Scope == EventScope.Global
                || (e.Scope == EventScope.CourseSection && e.CourseSectionId.HasValue && enrolledSectionIds.Contains(e.CourseSectionId.Value)));
        }
        else if (role == UserRole.Profesor && userId.HasValue)
        {
            var teacherId = await db.Teachers
                .Where(t => t.UserId == userId.Value)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(ct);

            var assignedSectionIds = await db.TeacherAssignments
                .Where(a => a.TeacherId == teacherId && a.IsCurrent)
                .Select(a => a.CourseSectionId)
                .Distinct()
                .ToListAsync(ct);

            query = query.Where(e =>
                e.Scope == EventScope.Global
                || e.CreatedByUserId == userId.Value
                || (e.Scope == EventScope.CourseSection && e.CourseSectionId.HasValue && assignedSectionIds.Contains(e.CourseSectionId.Value)));
        }

        return query;
    }

    private class EventDto
    {
        public int Id { get; init; }
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string EventType { get; init; } = "";
        public DateOnly EventDate { get; init; }
        public TimeOnly? StartTime { get; init; }
        public EventScope Scope { get; init; }
        public string? Modality { get; init; }
        public int? CourseSectionId { get; init; }
        public string? CourseName { get; init; }
        public string? CreatedByName { get; init; }
        public long CreatedByUserId { get; init; }
    }

    private class EventResponse
    {
        public int Id { get; init; }
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string EventType { get; init; } = "";
        public string Date { get; init; } = "";
        public string? StartTime { get; init; }
        public string Scope { get; init; } = "";
        public string? Modality { get; init; }
        public int? CourseSectionId { get; init; }
        public string? CourseName { get; init; }
        public string? CreatedByName { get; init; }
        public long CreatedByUserId { get; init; }
        public bool IsHoliday { get; init; }
    }
}

public record CreateEventRequest(
    string Title,
    string? Description,
    string Date,
    string? StartTime,
    string? EventType,
    int? CourseSectionId,
    string? Modality);
