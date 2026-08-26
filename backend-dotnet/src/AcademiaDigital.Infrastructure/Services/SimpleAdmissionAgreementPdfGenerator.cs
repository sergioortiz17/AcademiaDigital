using System.Globalization;
using System.Text;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class SimpleAdmissionAgreementPdfGenerator : IAdmissionAgreementPdfGenerator
{
    public Task<byte[]> GenerateAsync(AdmissionAgreementPdfModel model, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var lines = new List<string>
        {
            "ACADEMIA DIGITAL - ACUERDO DE ADMISION",
            $"Numero: {model.AgreementNumber}",
            $"Confirmado: {model.ConfirmedAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)}",
            $"Formulario: {model.FormTitle}",
            $"Carrera: {model.CareerName}",
            $"Postulante: {model.ApplicantEmail}",
            $"DNI: {model.ApplicantDni}",
            string.Empty,
            "Terminos aceptados:"
        };
        lines.AddRange(Wrap(model.TermsText, 92));
        lines.Add(string.Empty);
        lines.Add("Datos declarados:");
        foreach (var field in model.SubmittedFields.OrderBy(field => field.Key))
            lines.AddRange(Wrap($"{field.Key}: {field.Value}", 92));

        var commands = new StringBuilder("BT\n/F1 10 Tf\n50 790 Td\n");
        foreach (var line in lines.Take(48))
        {
            commands.Append('(').Append(Escape(ToAscii(line))).Append(") Tj\n0 -15 Td\n");
        }
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
        foreach (var offset in offsets.Skip(1))
            Write(stream, $"{offset:0000000000} 00000 n \n");
        Write(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static IEnumerable<string> Wrap(string value, int width)
    {
        var words = ToAscii(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        foreach (var word in words)
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > width)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word.Length > width ? word[..width] : word);
        }
        if (line.Length > 0) yield return line.ToString();
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string ToAscii(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Select(character => character <= 127 ? character : '?')
            .ToArray());
    }
}
