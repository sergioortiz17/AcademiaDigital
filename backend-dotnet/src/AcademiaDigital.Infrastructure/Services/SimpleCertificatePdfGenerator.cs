using System.Globalization;
using System.Reflection;
using System.Text;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Infrastructure.Services;

/// <summary>
/// Genera certificados/constancias con un diseño institucional (estilo ITSC): marco de página,
/// logo, encabezado con el nombre del instituto, título del certificado subrayado, cuerpo en
/// párrafo, tabla académica opcional, código de validación y pie con firma/sello.
///
/// El PDF se construye a mano (PDF 1.4) sin librerías externas. El logo se embebe como un
/// XObject de imagen RGB comprimido con Flate desde el recurso incrustado
/// <c>Assets/itsc-logo.rgbz</c>.
/// </summary>
public sealed class SimpleCertificatePdfGenerator : ICertificatePdfGenerator
{
    private const string InstitutionName = "Instituto Tecnológico Superior Córdoba";

    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double Margin = 40;
    private const double InnerMargin = 60;

    private static readonly Lazy<LogoImage?> Logo = new(LoadLogo);

    public Task<byte[]> GenerateAsync(CertificatePdfModel model, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var isTeacher = model.CertificateType.Contains("docente", StringComparison.OrdinalIgnoreCase);

        var content = new StringBuilder();
        var logo = Logo.Value;

        // 1) Marco de página.
        DrawFrame(content);

        // 2) Logo centrado arriba.
        var headerTop = PageHeight - Margin - 24;
        if (logo is not null)
        {
            const double logoDisplayWidth = 78;
            var logoDisplayHeight = logoDisplayWidth * logo.Height / logo.Width;
            var logoX = (PageWidth - logoDisplayWidth) / 2;
            var logoY = PageHeight - Margin - 20 - logoDisplayHeight;
            content.Append(CultureInfo.InvariantCulture,
                $"q\n{F(logoDisplayWidth)} 0 0 {F(logoDisplayHeight)} {F(logoX)} {F(logoY)} cm\n/Img0 Do\nQ\n");
            headerTop = logoY - 22;
        }

        // 3) Encabezado institucional.
        var cursorY = headerTop;
        cursorY = CenteredText(content, InstitutionName.ToUpperInvariant(), "F2", 14, cursorY);
        cursorY -= 16;
        cursorY = CenteredText(content, isTeacher ? "Secretaría Académica" : "Secretaría Académica", "F1", 10, cursorY);
        cursorY -= 40;

        // 4) Título del certificado (subrayado y centrado).
        var title = ToTitleCase(model.CertificateType);
        var titleWidth = MeasureText(title, 16, bold: true);
        var titleX = (PageWidth - titleWidth) / 2;
        CenteredText(content, title, "F2", 16, cursorY);
        content.Append(CultureInfo.InvariantCulture,
            $"{F(titleX)} {F(cursorY - 5)} m {F(titleX + titleWidth)} {F(cursorY - 5)} l 0.6 w S\n");
        cursorY -= 44;

        // 5) Cuerpo: párrafo con los datos.
        var body = BuildBody(model, isTeacher);
        var textWidth = PageWidth - (2 * InnerMargin);
        cursorY = WrappedText(content, body, "F1", 10.5, InnerMargin, cursorY, textWidth, 15);
        cursorY -= 20;

        // 6) Mesa de examen (si aplica).
        if (model.Exam is not null)
        {
            cursorY = LeftText(content, "Mesa de examen", "F2", 11, InnerMargin, cursorY);
            cursorY -= 16;
            cursorY = LeftText(content, $"{model.Exam.CourseCode} - {model.Exam.CourseName}", "F1", 10, InnerMargin, cursorY);
            cursorY -= 14;
            cursorY = LeftText(content,
                $"Fecha: {model.Exam.ExamDateUtc:dd/MM/yyyy HH:mm} UTC  -  Lugar: {model.Exam.Location}  -  Llamado: {model.Exam.CallNumber}",
                "F1", 10, InnerMargin, cursorY);
            cursorY -= 22;
        }

        // 7) Detalle académico (tabla) si hay materias.
        if (model.Courses.Count > 0)
        {
            cursorY = LeftText(content, "Detalle académico", "F2", 11, InnerMargin, cursorY);
            cursorY -= 6;
            content.Append(CultureInfo.InvariantCulture,
                $"{F(InnerMargin)} {F(cursorY)} m {F(PageWidth - InnerMargin)} {F(cursorY)} l 0.5 w S\n");
            cursorY -= 15;

            // Columnas: Código | Materia | Ciclo | Estado | Nota
            var xCode = InnerMargin;
            var xName = InnerMargin + 70;
            var xCycle = PageWidth - InnerMargin - 150;
            var xStatus = PageWidth - InnerMargin - 95;
            var xGrade = PageWidth - InnerMargin - 30;

            LeftText(content, "Código", "F2", 8.5, xCode, cursorY);
            LeftText(content, "Materia", "F2", 8.5, xName, cursorY);
            LeftText(content, "Ciclo", "F2", 8.5, xCycle, cursorY);
            LeftText(content, "Estado", "F2", 8.5, xStatus, cursorY);
            LeftText(content, "Nota", "F2", 8.5, xGrade, cursorY);
            cursorY -= 14;

            foreach (var course in model.Courses.Take(28))
            {
                LeftText(content, Trim(course.Code, 12), "F1", 8.5, xCode, cursorY);
                LeftText(content, Trim(course.Name, 34), "F1", 8.5, xName, cursorY);
                LeftText(content, $"{course.AcademicYear}/{course.Semester}", "F1", 8.5, xCycle, cursorY);
                LeftText(content, Trim(TranslateStatus(course.Status), 12), "F1", 8.5, xStatus, cursorY);
                LeftText(content, course.FinalGrade?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-", "F1", 8.5, xGrade, cursorY);
                cursorY -= 13;
            }
            cursorY -= 6;
            content.Append(CultureInfo.InvariantCulture,
                $"{F(InnerMargin)} {F(cursorY)} m {F(PageWidth - InnerMargin)} {F(cursorY)} l 0.5 w S\n");
            cursorY -= 20;
        }

        // 8) Código de validación.
        var validationCode = BuildValidationCode(model);
        CenteredText(content, $"CÓDIGO DE VALIDACIÓN: {validationCode}", "F2", 10, cursorY);

        // 9) Pie: firma, sello y leyenda de validación.
        var footerY = Margin + 54;
        content.Append(CultureInfo.InvariantCulture,
            $"{F(InnerMargin)} {F(footerY + 18)} m {F(PageWidth - InnerMargin)} {F(footerY + 18)} l 0.5 w S\n");
        CenteredText(content, model.SignatureText, "F1", 9, footerY);
        CenteredText(content, model.SealText, "F1", 9, footerY - 13);
        CenteredText(content,
            "Este documento puede ser validado ante la Secretaría Académica del instituto citando el código de validación.",
            "F1", 8, footerY - 28);

        return Task.FromResult(BuildPdf(content.ToString(), logo, validationCode));
    }

    private static string BuildBody(CertificatePdfModel model, bool isTeacher)
    {
        var issued = model.IssuedAt.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("es-AR"));
        if (isTeacher)
        {
            return
                $"Se deja constancia que {model.StudentName}, con DNI {model.Dni} y legajo N.º {model.LegajoNumber}, " +
                $"se desempeña como docente en actividad en el área de {model.CareerName}. " +
                $"Se extiende la presente constancia bajo el número {model.CertificateNumber} " +
                $"a los {issued}, para ser presentada ante quien corresponda.";
        }

        var kind = model.CertificateType.ToLowerInvariant();
        string statement;
        if (kind.Contains("regular"))
            statement = "es alumno regular";
        else if (kind.Contains("egreso"))
            statement = "ha finalizado sus estudios y es egresado";
        else if (kind.Contains("promedio"))
            statement = "registra el promedio académico que se detalla";
        else if (kind.Contains("examen") || kind.Contains("exámen"))
            statement = "se encuentra habilitado para rendir examen";
        else
            statement = "registra el siguiente estado académico";

        return
            $"Se deja constancia que {model.StudentName}, con DNI {model.Dni} y legajo N.º {model.LegajoNumber}, " +
            $"{statement} en la carrera {model.CareerName} de este instituto. " +
            $"Se extiende la presente constancia bajo el número {model.CertificateNumber} " +
            $"a los {issued}, para ser presentada ante quien corresponda.";
    }

    private static void DrawFrame(StringBuilder content)
    {
        content.Append(CultureInfo.InvariantCulture,
            $"0.15 0.22 0.29 RG\n1.5 w\n{F(Margin)} {F(Margin)} {F(PageWidth - 2 * Margin)} {F(PageHeight - 2 * Margin)} re S\n");
        var inset = Margin + 6;
        content.Append(CultureInfo.InvariantCulture,
            $"0.5 w\n{F(inset)} {F(inset)} {F(PageWidth - 2 * inset)} {F(PageHeight - 2 * inset)} re S\n");
        content.Append("0 0 0 RG\n");
    }

    private static double CenteredText(StringBuilder content, string text, string font, double size, double y)
    {
        var width = MeasureText(text, size, bold: font == "F2");
        var x = (PageWidth - width) / 2;
        Emit(content, SanitizePreservingLatin(text), font, size, x, y);
        return y;
    }

    private static double LeftText(StringBuilder content, string text, string font, double size, double x, double y)
    {
        Emit(content, SanitizePreservingLatin(text), font, size, x, y);
        return y;
    }

    private static double WrappedText(
        StringBuilder content, string text, string font, double size,
        double x, double y, double maxWidth, double lineHeight)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (MeasureText(candidate, size, bold: font == "F2") > maxWidth && line.Length > 0)
            {
                Emit(content, SanitizePreservingLatin(line.ToString()), font, size, x, y);
                y -= lineHeight;
                line.Clear();
                line.Append(word);
            }
            else
            {
                line.Clear();
                line.Append(candidate);
            }
        }
        if (line.Length > 0)
        {
            Emit(content, SanitizePreservingLatin(line.ToString()), font, size, x, y);
            y -= lineHeight;
        }
        return y;
    }

    private static void Emit(StringBuilder content, string text, string font, double size, double x, double y)
    {
        content.Append(CultureInfo.InvariantCulture,
            $"BT\n/{font} {F(size)} Tf\n{F(x)} {F(y)} Td\n({Escape(text)}) Tj\nET\n");
    }

    private static byte[] BuildPdf(string pageCommands, LogoImage? logo, string validationCode)
    {
        var contentBytes = Encoding.Latin1.GetBytes(pageCommands);

        var resources = new StringBuilder("<< /Font << /F1 5 0 R /F2 6 0 R >>");
        if (logo is not null) resources.Append(" /XObject << /Img0 7 0 R >>");
        resources.Append(" >>");

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(PageWidth)} {F(PageHeight)}] " +
            $"/Resources {resources} /Contents 4 0 R >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{pageCommands}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        if (logo is not null)
        {
            offsets.Add(stream.Position);
            Write(stream,
                $"7 0 obj\n<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} " +
                "/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode " +
                $"/Length {logo.CompressedRgb.Length} >>\nstream\n");
            stream.Write(logo.CompressedRgb, 0, logo.CompressedRgb.Length);
            Write(stream, "\nendstream\nendobj\n");
        }

        var objectCount = offsets.Count - 1;
        var xrefOffset = stream.Position;
        Write(stream, $"xref\n0 {objectCount + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(stream, $"{offset:0000000000} 00000 n \n");
        Write(stream,
            $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R " +
            $"/Info << /Title (Certificado {Escape(validationCode)}) >> >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string BuildValidationCode(CertificatePdfModel model)
    {
        var seed = $"{model.CertificateNumber}|{model.Dni}|{model.IssuedAt:yyyyMMddHHmm}";
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var digits = new StringBuilder();
        foreach (var b in hash)
        {
            digits.Append((b % 10).ToString(CultureInfo.InvariantCulture));
            if (digits.Length >= 14) break;
        }
        return digits.ToString();
    }

    private static string TranslateStatus(string status) => status.ToLowerInvariant() switch
    {
        "approved" => "Aprobada",
        "promoted" => "Promovida",
        "enrolled" => "Cursando",
        "regularized" => "Regular",
        "failed" => "Desaprobada",
        "withdrawn" => "Baja",
        _ => status
    };

    private static string ToTitleCase(string value)
    {
        var lower = value.ToLower(new CultureInfo("es-AR"));
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lower);
    }

    private static double MeasureText(string text, double size, bool bold)
    {
        double total = 0;
        foreach (var ch in text)
        {
            double width = ch switch
            {
                ' ' => 278,
                'i' or 'j' or 'l' or 'I' or '.' or ',' or '\'' or '!' or '|' or ':' or ';' => 278,
                'f' or 't' or 'r' => 333,
                'm' or 'M' or 'W' or 'w' => 833,
                >= 'A' and <= 'Z' => 667,
                >= '0' and <= '9' => 556,
                _ => 556
            };
            if (bold) width *= 1.06;
            total += width;
        }
        return total * size / 1000.0;
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string Trim(string value, int length) =>
        value.Length <= length ? value : value[..(length - 3)] + "...";

    private static string SanitizePreservingLatin(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(ch <= 0xFF ? ch : '?');
        return builder.ToString();
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static LogoImage? LoadLogo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("itsc-logo.rgbz", StringComparison.OrdinalIgnoreCase));
            if (resourceName is null) return null;

            using var resource = assembly.GetManifestResourceStream(resourceName);
            if (resource is null) return null;
            using var memory = new MemoryStream();
            resource.CopyTo(memory);
            var bytes = memory.ToArray();
            if (bytes.Length < 8) return null;

            var width = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
            var height = (bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7];
            if (width <= 0 || height <= 0) return null;

            var compressed = new byte[bytes.Length - 8];
            Array.Copy(bytes, 8, compressed, 0, compressed.Length);
            return new LogoImage(width, height, compressed);
        }
        catch
        {
            return null;
        }
    }

    private sealed record LogoImage(int Width, int Height, byte[] CompressedRgb);
}
