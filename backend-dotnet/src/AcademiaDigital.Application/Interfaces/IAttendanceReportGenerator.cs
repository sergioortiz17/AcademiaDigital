namespace AcademiaDigital.Application.Interfaces;

public sealed record AttendanceReportRow(
    string LegajoNumber,
    string Dni,
    string StudentName,
    string Status,
    string Notes,
    string Justification);

public sealed record AttendanceReportModel(
    long SessionId,
    string Course,
    string Commission,
    DateOnly SessionDate,
    string Scope,
    int Units,
    IReadOnlyList<AttendanceReportRow> Rows);

public sealed record AttendanceReportFile(byte[] Content, string ContentType, string FileName);

public interface IAttendanceReportGenerator
{
    Task<AttendanceReportFile> GenerateAsync(
        AttendanceReportModel model,
        string format,
        CancellationToken ct = default);
}
