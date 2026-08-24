using System.Globalization;
using System.Text;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class SimpleReceiptPdfGenerator : IReceiptPdfGenerator
{
    public Task<byte[]> GenerateAsync(ReceiptPdfModel model, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var lines = new List<string>
        {
            model.InstitutionName.ToUpperInvariant(),
            "COMPROBANTE DE PAGO",
            model.NonFiscalLegend,
            $"Numero: {model.ReceiptNumber}",
            $"Fecha: {model.IssuedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)}",
            string.Empty,
            $"Alumno: {model.StudentName}",
            $"DNI: {model.Dni}",
            $"Medio de pago: {model.PaymentMethod}",
            $"Total: {model.Currency} {model.Amount.ToString("0.00", CultureInfo.InvariantCulture)}",
            string.Empty,
            "Conceptos imputados:"
        };
        lines.AddRange(model.Items.Select(item =>
            $"{item.ConceptCode} - {item.ConceptName} | {model.Currency} {item.Amount.ToString("0.00", CultureInfo.InvariantCulture)}"));
        lines.Add(string.Empty);
        lines.Add($"Operador: {model.OperatorName} (ID {model.OperatorUserId})");
        lines.Add("Documento interno emitido por Academia Digital.");

        var commands = new StringBuilder("BT\n/F1 9 Tf\n35 805 Td\n");
        foreach (var line in lines.Take(52))
            commands.Append('(').Append(Escape(ToAscii(Trim(line, 105)))).Append(") Tj\n0 -14 Td\n");
        commands.Append("ET\n");
        return Task.FromResult(BuildPdf(commands.ToString()));
    }

    private static byte[] BuildPdf(string pageCommands)
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

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Trim(string value, int length) => value.Length <= length ? value : value[..length];
    private static string ToAscii(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Select(character => character <= 127 ? character : '?').ToArray());
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
