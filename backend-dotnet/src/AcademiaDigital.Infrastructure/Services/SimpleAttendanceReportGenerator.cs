using System.Globalization;
using System.Text;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class SimpleAttendanceReportGenerator : IAttendanceReportGenerator
{
    public Task<AttendanceReportFile> GenerateAsync(
        AttendanceReportModel model,
        string format,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var normalized = format.Trim().ToLowerInvariant();
        var date = model.SessionDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return normalized switch
        {
            "csv" or "excel" => Task.FromResult(new AttendanceReportFile(
                BuildCsv(model),
                "text/csv; charset=utf-8",
                $"attendance-{model.SessionId}-{date}.csv")),
            "pdf" => Task.FromResult(new AttendanceReportFile(
                BuildPdf(model),
                "application/pdf",
                $"attendance-{model.SessionId}-{date}.pdf")),
            _ => throw new ArgumentException("Attendance export format must be csv or pdf.")
        };
    }

    private static byte[] BuildCsv(AttendanceReportModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Legajo,DNI,Alumno,Estado,Observaciones,Justificacion");
        foreach (var row in model.Rows)
            builder.AppendLine(string.Join(',', new[]
            {
                Csv(row.LegajoNumber), Csv(row.Dni), Csv(row.StudentName),
                Csv(row.Status), Csv(row.Notes), Csv(row.Justification)
            }));
        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
            .ToArray();
    }

    private static byte[] BuildPdf(AttendanceReportModel model)
    {
        var lines = new List<string>
        {
            "ACADEMIA DIGITAL - PLANILLA DE ASISTENCIA",
            $"Sesion: {model.SessionId}",
            $"Fecha: {model.SessionDate:yyyy-MM-dd}",
            $"Materia: {model.Course}",
            $"Comision: {model.Commission}",
            $"Modalidad: {model.Scope} - Unidades: {model.Units}",
            string.Empty,
            "Legajo | DNI | Alumno | Estado"
        };
        lines.AddRange(model.Rows.Select(row =>
            $"{row.LegajoNumber} | {row.Dni} | {row.StudentName} | {row.Status}"));

        var commands = new StringBuilder("BT\n/F1 9 Tf\n35 800 Td\n");
        foreach (var line in lines.Take(52))
            commands.Append('(').Append(Escape(ToAscii(Trim(line, 105)))).Append(") Tj\n0 -14 Td\n");
        commands.Append("ET\n");
        return BuildPdfDocument(commands.ToString());
    }

    private static byte[] BuildPdfDocument(string pageCommands)
    {
        var contentBytes = Encoding.ASCII.GetBytes(pageCommands);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{pageCommands}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xrefOffset = stream.Position;
        Write(stream, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(stream, $"{offset:0000000000} 00000 n \n");
        Write(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Trim(string value, int length) => value.Length <= length ? value : value[..length];

    private static string ToAscii(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Select(character => character <= 127 ? character : '?')
            .ToArray());
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
