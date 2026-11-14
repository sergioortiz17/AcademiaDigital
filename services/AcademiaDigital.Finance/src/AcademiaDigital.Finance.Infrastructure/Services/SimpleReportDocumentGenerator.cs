using System.Globalization;
using System.Text;
using System.Xml.Linq;
using AcademiaDigital.Finance.Application.Interfaces;

namespace AcademiaDigital.Finance.Infrastructure.Services;

public sealed class SimpleReportDocumentGenerator : IReportDocumentGenerator
{
    public GeneratedReport GenerateExcel(
        string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string fileName)
    {
        XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";
        var table = new XElement(ss + "Table",
            new XElement(ss + "Row", new XElement(ss + "Cell",
                new XElement(ss + "Data", new XAttribute(ss + "Type", "String"), title))),
            new XElement(ss + "Row", headers.Select(value => Cell(ss, value))),
            rows.Select(row => new XElement(ss + "Row", row.Select(value => Cell(ss, value)))));
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(ss + "Workbook", new XAttribute(XNamespace.Xmlns + "ss", ss),
                new XElement(ss + "Worksheet", new XAttribute(ss + "Name", "Reporte"), table)));
        return new(Encoding.UTF8.GetBytes(document.ToString()), "application/vnd.ms-excel", EnsureExtension(fileName, ".xls"));
    }

    public GeneratedReport GeneratePdf(
        string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string fileName)
    {
        var lines = new List<string> { title, string.Join(" | ", headers) };
        lines.AddRange(rows.Take(45).Select(row => string.Join(" | ", row)));
        var stream = new StringBuilder("BT /F1 9 Tf 40 800 Td 12 TL ");
        foreach (var line in lines)
            stream.Append('(').Append(EscapePdf(FoldAscii(line, 110))).Append(") Tj T* ");
        stream.Append("ET");
        var content = stream.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            Write(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return new(output.ToArray(), "application/pdf", EnsureExtension(fileName, ".pdf"));
    }

    private static XElement Cell(XNamespace ns, string value)
        => new(ns + "Cell", new XElement(ns + "Data", new XAttribute(ns + "Type", "String"), value));

    private static string EnsureExtension(string fileName, string extension)
        => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? fileName : fileName + extension;

    private static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string FoldAscii(string value, int max)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var ascii = new string(normalized.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && character <= 127).ToArray());
        return ascii.Length <= max ? ascii : ascii[..(max - 3)] + "...";
    }

    private static void Write(Stream output, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        output.Write(bytes);
    }
}
