using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class StudentManagementService(Persistence.AppDbContext db) : IStudentManagementService
{
    private static readonly string[] Shifts = ["Morning", "Afternoon", "Evening"];
    private static readonly string[] ContentTypes = ["application/pdf", "image/jpeg", "image/png"];

    public async Task<PagedResult<StudentListItemDto>> SearchStudentsAsync(string? search, int? careerId,
        StudentStatus? status, int? academicYear, int? commissionId, int page, int pageSize, CancellationToken ct)
    {
        ValidatePage(ref page, ref pageSize);
        var query = db.Students.AsNoTracking().Include(x => x.User).Include(x => x.Career).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => (x.User.Dni != null && x.User.Dni.Contains(term)) ||
                x.User.Username.Contains(term) || x.User.LastName.Contains(term) ||
                (x.User.Username + " " + x.User.LastName).Contains(term));
        }
        if (careerId.HasValue) query = query.Where(x => db.StudentCareers.Any(sc => sc.StudentId == x.Id &&
            sc.CareerId == careerId && sc.IsActive));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (academicYear.HasValue)
            query = query.Where(x => db.StudentAcademicAssignments.Any(a => a.StudentId == x.Id && a.AcademicYear == academicYear));
        if (commissionId.HasValue)
            query = query.Where(x => db.StudentAcademicAssignments.Any(a => a.StudentId == x.Id && a.CommissionId == commissionId));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.User.LastName).ThenBy(x => x.User.Username)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToList();
        var assignments = await db.StudentAcademicAssignments.AsNoTracking().Include(x => x.Commission)
            .Where(x => ids.Contains(x.StudentId) && x.IsCurrent && x.CareerId == x.Student.CareerId)
            .ToDictionaryAsync(x => x.StudentId, ct);
        var items = rows.Select(x =>
        {
            assignments.TryGetValue(x.Id, out var a);
            return MapStudent(x, a);
        }).ToList();
        return new(items, page, pageSize, total);
    }

    public async Task<StudentListItemDto> UpdateStudentAsync(long id, UpdateStudentRequest r, CancellationToken ct)
    {
        var student = await db.Students.Include(x => x.User).Include(x => x.Career).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        var legajo = r.LegajoNumber.Trim();
        if (await db.Students.AnyAsync(x => x.Id != id && x.LegajoNumber == legajo, ct))
            throw new InvalidOperationException("El número de legajo ya existe.");
        student.LegajoNumber = legajo;
        student.AddressLine = Clean(r.AddressLine); student.City = Clean(r.City); student.Province = Clean(r.Province);
        student.PostalCode = Clean(r.PostalCode); student.EmergencyContactName = Clean(r.EmergencyContactName);
        student.EmergencyContactRelationship = Clean(r.EmergencyContactRelationship);
        student.EmergencyContactPhone = Clean(r.EmergencyContactPhone); student.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var assignment = await db.StudentAcademicAssignments.AsNoTracking().Include(x => x.Commission)
            .SingleOrDefaultAsync(x => x.StudentId == id && x.IsCurrent && x.CareerId == x.Student.CareerId, ct);
        return MapStudent(student, assignment);
    }

    public async Task<StatusHistoryDto> ChangeStatusAsync(long id, StudentStatus status, string reason, long actorId, CancellationToken ct)
    {
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        if (student.Status == status) throw new InvalidOperationException("El alumno ya tiene el estado solicitado.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("El motivo es obligatorio.");
        var history = new StudentStatusHistory { StudentId = id, PreviousStatus = student.Status, NewStatus = status,
            Reason = reason.Trim(), ChangedByUserId = actorId, ChangedAt = DateTime.UtcNow };
        student.Status = status; student.UpdatedAt = history.ChangedAt;
        db.StudentStatusHistory.Add(history);
        await db.SaveChangesAsync(ct);
        return Map(history);
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        return await db.StudentStatusHistory.AsNoTracking().Where(x => x.StudentId == id)
            .OrderByDescending(x => x.ChangedAt).Select(x => new StatusHistoryDto(x.Id, x.PreviousStatus, x.NewStatus,
                x.Reason, x.ChangedAt, x.ChangedByUserId)).ToListAsync(ct);
    }

    public async Task SoftDeleteStudentAsync(long id, string reason, long actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await ChangeStatusAsync(id, StudentStatus.Withdrawn, reason, actorId, ct);
        var assignments = await db.StudentAcademicAssignments.Where(x => x.StudentId == id && x.IsCurrent).ToListAsync(ct);
        foreach (var a in assignments) { a.IsCurrent = false; a.EndedAt = DateTime.UtcNow; }
        var memberships = await db.StudentCareers.Where(x => x.StudentId == id && x.IsActive).ToListAsync(ct);
        foreach (var membership in memberships) { membership.IsActive = false; membership.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    public async Task<StudentRecordDto> GetRecordAsync(long id, CancellationToken ct)
    {
        var s = await db.Students.AsNoTracking().Include(x => x.User).Include(x => x.Career)
            .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Alumno no encontrado.");
        var assignment = await db.StudentAcademicAssignments.AsNoTracking().Include(x => x.Commission)
            .SingleOrDefaultAsync(x => x.StudentId == id && x.IsCurrent && x.CareerId == s.CareerId, ct);
        var careerIds = await db.StudentCareers.AsNoTracking().Where(x => x.StudentId == id && x.IsActive)
            .Select(x => x.CareerId).ToListAsync(ct);
        var required = await ApplicableRequirements(careerIds).CountAsync(ct);
        var approved = await db.StudentDocuments.Where(x => x.StudentId == id && x.Status == StudentDocumentStatus.Approved)
            .Select(x => x.DocumentRequirementId).Distinct().CountAsync(ct);
        var scholarships = await GetStudentScholarshipsAsync(id, ct);
        var custom = await GetCustomValuesAsync(id, ct);
        return new(
            new { s.Id, s.LegajoNumber, Status = s.Status.ToString(), s.EnrollmentDate },
            new { Dni = s.User.Dni, FirstName = s.User.Username, s.User.LastName, s.User.Email, s.User.Cuil, s.User.BirthDate,
                Phone = $"{s.User.PhoneCode}{s.User.Phone}" },
            new { s.AddressLine, s.City, s.Province, s.PostalCode },
            new { Name = s.EmergencyContactName, Relationship = s.EmergencyContactRelationship, Phone = s.EmergencyContactPhone },
            assignment is null ? null : Map(assignment),
            new { Required = required, Approved = approved, Pending = Math.Max(0, required - approved) },
            scholarships.Where(x => x.Status == StudentScholarshipStatus.Granted).ToList(), custom);
    }

    public Task<bool> IsOwnerAsync(long studentId, long userId, CancellationToken ct)
        => db.Students.AnyAsync(x => x.Id == studentId && x.UserId == userId, ct);

    public async Task<IReadOnlyList<StudentCareerDto>> GetStudentCareersAsync(long studentId, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        var memberships = await db.StudentCareers.AsNoTracking().Include(x => x.Career)
            .Where(x => x.StudentId == studentId).OrderByDescending(x => x.IsActive).ThenBy(x => x.EnrollmentDate).ToListAsync(ct);
        var plans = await db.StudentStudyPlans.AsNoTracking().Include(x => x.StudyPlan).Include(x => x.StudentCareer)
            .Where(x => x.StudentId == studentId && x.IsCurrent).ToDictionaryAsync(x => x.StudentCareerId, ct);
        return memberships.Select(x =>
        {
            plans.TryGetValue(x.Id, out var plan);
            return new StudentCareerDto(x.Id, x.CareerId, x.Career.Name, x.EnrollmentDate, x.IsActive,
                x.CareerId == student.CareerId, plan?.StudyPlanId, plan?.StudyPlan.Name);
        }).ToList();
    }

    public async Task<StudentCareerDto> AddStudentCareerAsync(long studentId, AddStudentCareerRequest r, CancellationToken ct)
    {
        await EnsureStudent(studentId, ct);
        var career = await db.Careers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == r.CareerId, ct)
            ?? throw new KeyNotFoundException("Carrera no encontrada.");
        if (!career.IsActive) throw new InvalidOperationException("La carrera está inactiva.");
        if (await db.StudentCareers.AnyAsync(x => x.StudentId == studentId && x.CareerId == r.CareerId, ct))
            throw new InvalidOperationException("El alumno ya está inscripto en esta carrera.");
        var membership = new StudentCareer
        {
            StudentId = studentId,
            CareerId = career.Id,
            EnrollmentDate = r.EnrollmentDate ?? DateTime.UtcNow
        };
        db.StudentCareers.Add(membership);
        await db.SaveChangesAsync(ct);
        return new StudentCareerDto(membership.Id, career.Id, career.Name, membership.EnrollmentDate,
            membership.IsActive, false, null, null);
    }

    public async Task<IReadOnlyList<CommissionDto>> GetCommissionsAsync(int careerId, int? year, CancellationToken ct)
    {
        var q = db.Commissions.AsNoTracking().Where(x => x.CareerId == careerId && x.IsActive);
        if (year.HasValue) q = q.Where(x => x.AcademicYear == year);
        return await q.OrderByDescending(x => x.AcademicYear).ThenBy(x => x.Code).Select(x =>
            new CommissionDto(x.Id, x.CareerId, x.Code, x.Name, x.AcademicYear, x.YearNumber, x.Shift, x.IsActive)).ToListAsync(ct);
    }

    public async Task<CommissionDto> SaveCommissionAsync(int careerId, int? id, UpsertCommissionRequest r, CancellationToken ct)
    {
        if (!await db.Careers.AnyAsync(x => x.Id == careerId, ct)) throw new KeyNotFoundException("Carrera no encontrada.");
        if (!Shifts.Contains(r.Shift, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Turno inválido.");
        Commission item;
        if (id.HasValue) item = await db.Commissions.SingleOrDefaultAsync(x => x.Id == id && x.CareerId == careerId, ct)
            ?? throw new KeyNotFoundException("Comisión no encontrada.");
        else { item = new Commission { CareerId = careerId }; db.Commissions.Add(item); }
        var code = r.Code.Trim();
        if (await db.Commissions.AnyAsync(x => x.Id != item.Id && x.CareerId == careerId &&
            x.AcademicYear == r.AcademicYear && x.Code == code, ct)) throw new InvalidOperationException("El código de la comisión ya existe.");
        item.Code = code; item.Name = r.Name.Trim(); item.AcademicYear = r.AcademicYear;
        item.YearNumber = r.YearNumber; item.Shift = Shifts.Single(x => x.Equals(r.Shift, StringComparison.OrdinalIgnoreCase));
        await db.SaveChangesAsync(ct); return Map(item);
    }

    public async Task DisableCommissionAsync(int careerId, int id, CancellationToken ct)
    {
        var item = await db.Commissions.SingleOrDefaultAsync(x => x.Id == id && x.CareerId == careerId, ct)
            ?? throw new KeyNotFoundException("Comisión no encontrada.");
        if (await db.StudentAcademicAssignments.AnyAsync(x => x.CommissionId == id && x.IsCurrent, ct))
            throw new InvalidOperationException("La comisión tiene asignaciones vigentes.");
        item.IsActive = false; await db.SaveChangesAsync(ct);
    }

    public async Task<AcademicAssignmentDto> AssignAcademicAsync(long studentId, CreateAcademicAssignmentRequest r, long actorId, CancellationToken ct)
    {
        return await ExecuteAtomicAsync(async () =>
        {
            await EnsureStudent(studentId, ct);
            var membership = await db.StudentCareers.SingleOrDefaultAsync(x => x.StudentId == studentId &&
                x.CareerId == r.CareerId && x.IsActive, ct)
                ?? throw new InvalidOperationException("El alumno no está inscripto activamente en la carrera seleccionada.");
            var plan = await db.StudyPlans.SingleOrDefaultAsync(x => x.Id == r.StudyPlanId, ct)
                ?? throw new KeyNotFoundException("Plan de estudios no encontrado.");
            var commission = await db.Commissions.SingleOrDefaultAsync(x => x.Id == r.CommissionId && x.IsActive, ct)
                ?? throw new KeyNotFoundException("Comisión no encontrada.");
            if (plan.CareerId != r.CareerId || commission.CareerId != r.CareerId ||
                commission.AcademicYear != r.AcademicYear || commission.YearNumber != r.YearNumber)
                throw new InvalidOperationException("La carrera, el plan, la comisión y el ciclo académico son incompatibles.");
            var current = await db.StudentAcademicAssignments.Where(x => x.StudentCareerId == membership.Id && x.IsCurrent).ToListAsync(ct);
            foreach (var x in current) { x.IsCurrent = false; x.EndedAt = DateTime.UtcNow; }
            var plans = await db.StudentStudyPlans.Where(x => x.StudentCareerId == membership.Id && x.IsCurrent).ToListAsync(ct);
            foreach (var x in plans) { x.IsCurrent = false; x.EndedAt = DateTime.UtcNow; }
            var assignment = new StudentAcademicAssignment { StudentId = studentId, StudentCareerId = membership.Id,
                CareerId = r.CareerId, StudyPlanId = r.StudyPlanId, CommissionId = r.CommissionId,
                AcademicYear = r.AcademicYear, YearNumber = r.YearNumber, Reason = Clean(r.Reason), AssignedByUserId = actorId };
            db.StudentAcademicAssignments.Add(assignment);
            db.StudentStudyPlans.Add(new StudentStudyPlan { StudentId = studentId, StudentCareerId = membership.Id,
                StudyPlanId = r.StudyPlanId, MigrationReason = r.Reason });
            await db.SaveChangesAsync(ct);
            assignment.Commission = commission;
            return Map(assignment);
        }, ct);
    }

    public async Task<IReadOnlyList<AcademicAssignmentDto>> GetAssignmentsAsync(long id, int? year, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        var q = db.StudentAcademicAssignments.AsNoTracking().Include(x => x.Commission).Where(x => x.StudentId == id);
        if (year.HasValue) q = q.Where(x => x.AcademicYear == year);
        return (await q.OrderByDescending(x => x.StartedAt).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DocumentRequirementDto>> GetRequirementsAsync(int? careerId, CancellationToken ct)
        => await ApplicableRequirements(careerId).OrderBy(x => x.Name).Select(x => new DocumentRequirementDto(x.Id, x.Code,
            x.Name, x.Description, x.CareerId, x.IsRequired, x.IsActive, x.ValidFrom, x.ValidTo)).ToListAsync(ct);

    public async Task<DocumentRequirementDto> SaveRequirementAsync(int? id, UpsertDocumentRequirementRequest r, CancellationToken ct)
    {
        if (r.ValidFrom.HasValue && r.ValidTo < r.ValidFrom) throw new ArgumentException("Rango de vigencia inválido.");
        if (r.CareerId.HasValue && !await db.Careers.AnyAsync(x => x.Id == r.CareerId, ct)) throw new KeyNotFoundException("Carrera no encontrada.");
        DocumentRequirement x;
        if (id.HasValue) x = await db.DocumentRequirements.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Requisito no encontrado.");
        else { x = new(); db.DocumentRequirements.Add(x); }
        var code = r.Code.Trim();
        if (await db.DocumentRequirements.AnyAsync(y => y.Id != x.Id && y.Code == code, ct)) throw new InvalidOperationException("El código del requisito ya existe.");
        x.Code = code; x.Name = r.Name.Trim(); x.Description = Clean(r.Description); x.CareerId = r.CareerId;
        x.IsRequired = r.IsRequired; x.ValidFrom = r.ValidFrom; x.ValidTo = r.ValidTo;
        await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task DisableRequirementAsync(int id, CancellationToken ct)
    { var x = await db.DocumentRequirements.FindAsync([id], ct) ?? throw new KeyNotFoundException("Requisito no encontrado."); x.IsActive = false; await db.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<StudentDocumentDto>> GetDocumentsAsync(long id, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        return (await db.StudentDocuments.AsNoTracking().Include(x => x.DocumentRequirement).Where(x => x.StudentId == id)
            .OrderByDescending(x => x.SubmittedAt).ToListAsync(ct)).Select(Map).ToList();
    }
    public async Task<StudentDocumentDto> AddDocumentAsync(long id, CreateStudentDocumentRequest r, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        var req = await db.DocumentRequirements.FindAsync([r.DocumentRequirementId], ct) ?? throw new KeyNotFoundException("Requisito no encontrado.");
        if (!ContentTypes.Contains(r.ContentType)) throw new ArgumentException("Tipo de contenido no soportado.");
        var previous = await db.StudentDocuments.Where(x => x.StudentId == id &&
            x.DocumentRequirementId == r.DocumentRequirementId &&
            (x.Status == StudentDocumentStatus.Submitted || x.Status == StudentDocumentStatus.Approved)).ToListAsync(ct);
        foreach (var old in previous) old.Status = StudentDocumentStatus.Expired;
        var x = new StudentDocument { StudentId = id, DocumentRequirementId = r.DocumentRequirementId, FileUrl = r.FileUrl.Trim(),
            OriginalFileName = r.OriginalFileName.Trim(), ContentType = r.ContentType, FileSizeBytes = r.FileSizeBytes };
        db.StudentDocuments.Add(x); await db.SaveChangesAsync(ct); x.DocumentRequirement = req; return Map(x);
    }
    public async Task<StudentDocumentDto> ReviewDocumentAsync(long studentId, long documentId, ReviewStudentDocumentRequest r, long actorId, CancellationToken ct)
    {
        var x = await db.StudentDocuments.Include(y => y.DocumentRequirement).SingleOrDefaultAsync(y => y.Id == documentId && y.StudentId == studentId, ct)
            ?? throw new KeyNotFoundException("Documento no encontrado.");
        if (x.Status != StudentDocumentStatus.Submitted) throw new InvalidOperationException("Solo se pueden revisar documentos enviados.");
        if (r.Status is not (StudentDocumentStatus.Approved or StudentDocumentStatus.Rejected)) throw new ArgumentException("El estado de revisión debe ser Aprobado o Rechazado.");
        if (r.Status == StudentDocumentStatus.Rejected && string.IsNullOrWhiteSpace(r.Observation)) throw new ArgumentException("La observación es obligatoria al rechazar.");
        x.Status = r.Status; x.Observation = Clean(r.Observation); x.ReviewedAt = DateTime.UtcNow; x.ReviewedByUserId = actorId;
        await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task DeleteDocumentAsync(long studentId, long documentId, CancellationToken ct)
    {
        var x = await db.StudentDocuments.SingleOrDefaultAsync(y => y.Id == documentId && y.StudentId == studentId, ct)
            ?? throw new KeyNotFoundException("Documento no encontrado.");
        if (x.Status == StudentDocumentStatus.Approved) throw new InvalidOperationException("No se pueden eliminar documentos aprobados.");
        x.Status = StudentDocumentStatus.Expired; await db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<DocumentRequirementDto>> GetPendingDocumentsAsync(long id, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        var careerIds = await db.StudentCareers.AsNoTracking().Where(x => x.StudentId == id && x.IsActive)
            .Select(x => x.CareerId).ToListAsync(ct);
        var approved = db.StudentDocuments.Where(x => x.StudentId == id && x.Status == StudentDocumentStatus.Approved).Select(x => x.DocumentRequirementId);
        return await ApplicableRequirements(careerIds).Where(x => x.IsRequired && !approved.Contains(x.Id))
            .Select(x => new DocumentRequirementDto(x.Id, x.Code, x.Name, x.Description, x.CareerId, x.IsRequired, x.IsActive, x.ValidFrom, x.ValidTo)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ScholarshipDto>> GetScholarshipsAsync(CancellationToken ct)
        => await db.Scholarships.AsNoTracking().OrderBy(x => x.Name).Select(x => new ScholarshipDto(x.Id, x.Code, x.Name, x.Description, x.IsActive)).ToListAsync(ct);
    public async Task<ScholarshipDto> SaveScholarshipAsync(int? id, UpsertScholarshipRequest r, CancellationToken ct)
    {
        Scholarship x;
        if (id.HasValue) x = await db.Scholarships.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Beca no encontrada.");
        else { x = new(); db.Scholarships.Add(x); }
        var code = r.Code.Trim();
        if (await db.Scholarships.AnyAsync(y => y.Id != x.Id && y.Code == code, ct)) throw new InvalidOperationException("El código de la beca ya existe.");
        x.Code = code; x.Name = r.Name.Trim(); x.Description = Clean(r.Description); await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task DisableScholarshipAsync(int id, CancellationToken ct)
    { var x = await db.Scholarships.FindAsync([id], ct) ?? throw new KeyNotFoundException("Beca no encontrada."); x.IsActive = false; await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<StudentScholarshipDto>> GetStudentScholarshipsAsync(long id, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        return (await db.StudentScholarships.AsNoTracking().Include(x => x.Scholarship).Where(x => x.StudentId == id)
            .OrderByDescending(x => x.AcademicYear).ToListAsync(ct)).Select(Map).ToList();
    }
    public async Task<StudentScholarshipDto> SaveStudentScholarshipAsync(long studentId, long? id, UpsertStudentScholarshipRequest r, long actorId, CancellationToken ct)
    {
        await EnsureStudent(studentId, ct);
        var scholarship = await db.Scholarships.SingleOrDefaultAsync(x => x.Id == r.ScholarshipId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("Beca no encontrada.");
        if (r.ValidFrom.HasValue && r.ValidTo < r.ValidFrom) throw new ArgumentException("Rango de vigencia inválido.");
        StudentScholarship x;
        if (id.HasValue) x = await db.StudentScholarships.SingleOrDefaultAsync(y => y.Id == id && y.StudentId == studentId, ct)
            ?? throw new KeyNotFoundException("Beca del alumno no encontrada.");
        else { x = new() { StudentId = studentId }; db.StudentScholarships.Add(x); }
        x.ScholarshipId = r.ScholarshipId; x.AcademicYear = r.AcademicYear; x.Status = r.Status;
        x.ValidFrom = r.ValidFrom; x.ValidTo = r.ValidTo; x.Notes = Clean(r.Notes); x.UpdatedByUserId = actorId;
        if (r.Status == StudentScholarshipStatus.Granted && x.GrantedAt is null) x.GrantedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); x.Scholarship = scholarship; return Map(x);
    }
    public async Task RevokeStudentScholarshipAsync(long studentId, long id, long actorId, CancellationToken ct)
    {
        var x = await db.StudentScholarships.SingleOrDefaultAsync(y => y.Id == id && y.StudentId == studentId, ct)
            ?? throw new KeyNotFoundException("Beca del alumno no encontrada.");
        if (x.Status == StudentScholarshipStatus.Revoked) throw new InvalidOperationException("La beca ya está revocada.");
        x.Status = StudentScholarshipStatus.Revoked; x.UpdatedByUserId = actorId; await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> GetCustomFieldsAsync(CancellationToken ct)
        => (await db.CustomFieldDefinitions.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ToListAsync(ct)).Select(Map).ToList();
    public async Task<CustomFieldDefinitionDto> SaveCustomFieldAsync(int? id, UpsertCustomFieldRequest r, CancellationToken ct)
    {
        if (!Regex.IsMatch(r.Key, "^[a-z][a-z0-9_]*$")) throw new ArgumentException("Clave de campo personalizado inválida.");
        if (r.DataType == CustomFieldDataType.Select && (r.Options is null || r.Options.Count == 0)) throw new ArgumentException("Los campos de selección requieren opciones.");
        CustomFieldDefinition x;
        if (id.HasValue) x = await db.CustomFieldDefinitions.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException("Campo personalizado no encontrado.");
        else { x = new(); db.CustomFieldDefinitions.Add(x); }
        if (x.Id != 0 && x.DataType != r.DataType && await db.StudentCustomFieldValues.AnyAsync(v => v.CustomFieldDefinitionId == x.Id, ct))
            throw new InvalidOperationException("El tipo de dato no puede cambiar una vez que existen valores.");
        if (await db.CustomFieldDefinitions.AnyAsync(y => y.Id != x.Id && y.Key == r.Key, ct)) throw new InvalidOperationException("La clave del campo personalizado ya existe.");
        x.Key = r.Key; x.Label = r.Label.Trim(); x.DataType = r.DataType; x.IsRequired = r.IsRequired;
        x.OptionsJson = r.Options is null ? null : JsonSerializer.Serialize(r.Options); x.SortOrder = r.SortOrder;
        await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task DisableCustomFieldAsync(int id, CancellationToken ct)
    { var x = await db.CustomFieldDefinitions.FindAsync([id], ct) ?? throw new KeyNotFoundException("Campo personalizado no encontrado."); x.IsActive = false; await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyDictionary<string, object?>> GetCustomValuesAsync(long id, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        var values = await db.StudentCustomFieldValues.AsNoTracking().Include(x => x.CustomFieldDefinition)
            .Where(x => x.StudentId == id && x.CustomFieldDefinition.IsActive).ToListAsync(ct);
        return values.ToDictionary(x => x.CustomFieldDefinition.Key, x => ParseValue(x.Value, x.CustomFieldDefinition.DataType));
    }
    public async Task<IReadOnlyDictionary<string, object?>> SaveCustomValuesAsync(long id, UpsertCustomValuesRequest r, long actorId, CancellationToken ct)
    {
        await EnsureStudent(id, ct);
        var keys = r.Values.Keys.ToList();
        var defs = await db.CustomFieldDefinitions.Where(x => keys.Contains(x.Key) && x.IsActive).ToDictionaryAsync(x => x.Key, ct);
        if (defs.Count != keys.Count) throw new ArgumentException("Uno o más campos personalizados no existen.");
        var normalized = r.Values.ToDictionary(x => x.Key, x => NormalizeValue(x.Value, defs[x.Key]));
        var existing = await db.StudentCustomFieldValues.Where(x => x.StudentId == id && defs.Values.Select(d => d.Id).Contains(x.CustomFieldDefinitionId)).ToListAsync(ct);
        foreach (var pair in normalized)
        {
            var def = defs[pair.Key]; var value = existing.SingleOrDefault(x => x.CustomFieldDefinitionId == def.Id);
            if (value is null) { value = new() { StudentId = id, CustomFieldDefinitionId = def.Id }; db.StudentCustomFieldValues.Add(value); }
            value.Value = pair.Value; value.UpdatedAt = DateTime.UtcNow; value.UpdatedByUserId = actorId;
        }
        await db.SaveChangesAsync(ct); return await GetCustomValuesAsync(id, ct);
    }

    public async Task<PagedResult<AcademicHistoryItemDto>> GetAcademicHistoryAsync(long id, int? year, int page, int pageSize, CancellationToken ct)
    {
        await EnsureStudent(id, ct); ValidatePage(ref page, ref pageSize);
        var q = db.Enrollments.AsNoTracking().Include(x => x.Course).Where(x => x.StudentId == id);
        if (year.HasValue) q = q.Where(x => x.AcademicYear == year);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.AcademicYear).ThenByDescending(x => x.Semester)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AcademicHistoryItemDto(x.Id, x.AcademicYear,
                x.Semester, x.CourseId, x.Course.Code, x.Course.Name, x.Status.ToString(), x.FinalGrade, x.EnrollmentDate)).ToListAsync(ct);
        return new(items, page, pageSize, total);
    }

    private IQueryable<DocumentRequirement> ApplicableRequirements(int? careerId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.DocumentRequirements.AsNoTracking().Where(x => x.IsActive &&
            (!careerId.HasValue || x.CareerId == null || x.CareerId == careerId) &&
            (!x.ValidFrom.HasValue || x.ValidFrom <= today) && (!x.ValidTo.HasValue || x.ValidTo >= today));
    }
    private IQueryable<DocumentRequirement> ApplicableRequirements(IReadOnlyCollection<int> careerIds)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.DocumentRequirements.AsNoTracking().Where(x => x.IsActive &&
            (x.CareerId == null || careerIds.Contains(x.CareerId.Value)) &&
            (!x.ValidFrom.HasValue || x.ValidFrom <= today) && (!x.ValidTo.HasValue || x.ValidTo >= today));
    }
    private async Task<T> ExecuteAtomicAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null) return await operation();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw;
        }
    }
    private async Task EnsureStudent(long id, CancellationToken ct)
    { if (!await db.Students.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException("Alumno no encontrado."); }
    private static void ValidatePage(ref int page, ref int size)
    { if (page < 1 || size < 1 || size > 100) throw new ArgumentException("Paginación inválida."); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static StudentListItemDto MapStudent(Student s, StudentAcademicAssignment? a) => new(s.Id, s.UserId, s.User.Dni,
        $"{s.User.Username} {s.User.LastName}".Trim(), s.LegajoNumber, s.Status, s.CareerId, s.Career.Name,
        a?.AcademicYear, a?.YearNumber, a?.CommissionId, a?.Commission?.Name);
    private static StatusHistoryDto Map(StudentStatusHistory x) => new(x.Id, x.PreviousStatus, x.NewStatus, x.Reason, x.ChangedAt, x.ChangedByUserId);
    private static CommissionDto Map(Commission x) => new(x.Id, x.CareerId, x.Code, x.Name, x.AcademicYear, x.YearNumber, x.Shift, x.IsActive);
    private static AcademicAssignmentDto Map(StudentAcademicAssignment x) => new(x.Id, x.StudentId, x.CareerId, x.StudyPlanId,
        x.CommissionId, x.Commission?.Name, x.AcademicYear, x.YearNumber, x.StartedAt, x.EndedAt, x.IsCurrent, x.Reason);
    private static DocumentRequirementDto Map(DocumentRequirement x) => new(x.Id, x.Code, x.Name, x.Description, x.CareerId, x.IsRequired, x.IsActive, x.ValidFrom, x.ValidTo);
    private static StudentDocumentDto Map(StudentDocument x) => new(x.Id, x.StudentId, x.DocumentRequirementId,
        x.DocumentRequirement.Name, x.FileUrl, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.Status, x.SubmittedAt, x.ReviewedAt, x.Observation);
    private static ScholarshipDto Map(Scholarship x) => new(x.Id, x.Code, x.Name, x.Description, x.IsActive);
    private static StudentScholarshipDto Map(StudentScholarship x) => new(x.Id, x.StudentId, x.ScholarshipId, x.Scholarship.Name,
        x.AcademicYear, x.Status, x.GrantedAt, x.ValidFrom, x.ValidTo, x.Notes);
    private static CustomFieldDefinitionDto Map(CustomFieldDefinition x) => new(x.Id, x.Key, x.Label, x.DataType, x.IsRequired,
        x.OptionsJson is null ? null : JsonSerializer.Deserialize<List<string>>(x.OptionsJson), x.IsActive, x.SortOrder);
    private static string? NormalizeValue(object? raw, CustomFieldDefinition def)
    {
        if (raw is null) { if (def.IsRequired) throw new ArgumentException($"{def.Key} es obligatorio."); return null; }
        var text = raw is JsonElement e ? e.ToString() : Convert.ToString(raw, CultureInfo.InvariantCulture);
        return def.DataType switch
        {
            CustomFieldDataType.Text => text,
            CustomFieldDataType.Number when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) => n.ToString(CultureInfo.InvariantCulture),
            CustomFieldDataType.Date when DateOnly.TryParse(text, out var d) => d.ToString("yyyy-MM-dd"),
            CustomFieldDataType.Boolean when bool.TryParse(text, out var b) => b.ToString().ToLowerInvariant(),
            CustomFieldDataType.Select when JsonSerializer.Deserialize<List<string>>(def.OptionsJson ?? "[]")!.Contains(text!) => text,
            _ => throw new ArgumentException($"Valor inválido para {def.Key}.")
        };
    }
    private static object? ParseValue(string? value, CustomFieldDataType type)
    {
        if (value is null) return null;
        return type switch { CustomFieldDataType.Number => decimal.Parse(value, CultureInfo.InvariantCulture),
            CustomFieldDataType.Boolean => bool.Parse(value), _ => value };
    }
}
