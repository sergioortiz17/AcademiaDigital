namespace AcademiaDigital.Application.UseCases.Enrollments;

/// <summary>
/// Agrupa los handlers de períodos de inscripción para evitar exceder el límite
/// de parámetros en el constructor del controller.
/// </summary>
public sealed class EnrollmentPeriodFacade(
    GetAllEnrollmentPeriodsQueryHandler getAll,
    GetActiveEnrollmentPeriodQueryHandler getActive,
    GetEnrolledStudentsQueryHandler getStudents,
    OpenEnrollmentPeriodCommandHandler open,
    CloseEnrollmentPeriodCommandHandler close,
    UpdatePeriodQuotasCommandHandler updateQuotas,
    RemoveStudentFromPeriodCommandHandler removeStudent)
{
    public Task<IReadOnlyList<EnrollmentPeriodDto>> GetAllAsync(CancellationToken ct = default)
        => getAll.Handle(new GetAllEnrollmentPeriodsQuery(), ct);

    public Task<EnrollmentPeriodDto?> GetActiveAsync(int careerId, CancellationToken ct = default)
        => getActive.Handle(new GetActiveEnrollmentPeriodQuery(careerId), ct);

    public Task<(int Total, IReadOnlyList<EnrolledStudentDto> Students)> GetStudentsAsync(int periodId, CancellationToken ct = default)
        => getStudents.Handle(new GetEnrolledStudentsQuery(periodId), ct);

    public Task<EnrollmentPeriodDto> OpenAsync(OpenEnrollmentPeriodCommand cmd, CancellationToken ct = default)
        => open.Handle(cmd, ct);

    public Task CloseAsync(int periodId, CancellationToken ct = default)
        => close.Handle(new CloseEnrollmentPeriodCommand(periodId), ct);

    public Task<EnrollmentPeriodDto> UpdateQuotasAsync(UpdatePeriodQuotasCommand cmd, CancellationToken ct = default)
        => updateQuotas.Handle(cmd, ct);

    public Task RemoveStudentAsync(int periodId, long studentId, CancellationToken ct = default)
        => removeStudent.Handle(new RemoveStudentFromPeriodCommand(periodId, studentId), ct);
}

/// <summary>Facade for admin-only period lifecycle and reporting operations.</summary>
public sealed class EnrollmentPeriodAdminFacade(
    ActivateEnrollmentPeriodCommandHandler activate,
    DeleteEnrollmentPeriodCommandHandler delete,
    GetPeriodReportQueryHandler getReport)
{
    public Task ActivateAsync(int periodId, CancellationToken ct = default)
        => activate.Handle(new ActivateEnrollmentPeriodCommand(periodId), ct);

    public Task DeleteAsync(int periodId, CancellationToken ct = default)
        => delete.Handle(new DeleteEnrollmentPeriodCommand(periodId), ct);

    public Task<PeriodReportDto> GetReportAsync(int periodId, CancellationToken ct = default)
        => getReport.Handle(new GetPeriodReportQuery(periodId), ct);
}
