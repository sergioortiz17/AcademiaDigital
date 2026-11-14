namespace AcademiaDigital.Application.Interfaces;

public sealed record AcademicDirectoryStudent(
    long StudentId,
    long StudentCareerId,
    string StudentName,
    string Dni,
    string LegajoNumber,
    int CareerId,
    string CareerName,
    int? CommissionId,
    string? CommissionName,
    int AcademicYear,
    bool IsActive);

/// <summary>
/// Read-only projection used to share the minimum academic directory with Finance.
/// It never exposes the academic database or its entities to the financial service.
/// </summary>
public interface IAcademicDirectoryReader
{
    Task<IReadOnlyList<AcademicDirectoryStudent>> ReadAsync(CancellationToken ct = default);
}
