using System.Globalization;
using System.Reflection;
using System.Text;
using AcademiaDigital.Finance.Application.Interfaces;

namespace AcademiaDigital.Finance.Infrastructure.Services;

/// <summary>
/// Genera un comprobante de pago con un diseño institucional (estilo ITSC): marco de página,
/// logo, encabezado con el nombre del instituto, título, cuerpo con los datos del pago,
/// tabla de conceptos imputados, código de validación y pie no fiscal.
///
/// El PDF se construye a mano (PDF 1.4) para no depender de librerías externas. El logo se
/// embebe como un XObject de imagen RGB comprimido con Flate (zlib) desde el recurso
/// incrustado <c>Assets/itsc-logo.rgbz</c>.
/// </summary>
public sealed class SimpleReceiptPdfGenerator : IReceiptPdfGenerator
{
    // A4 en puntos PDF.
    private const double PageWidth = 595;
    private const double PageHeight = 842;

    // Márgenes del marco interior.
    private const double Margin = 40;
    private const double InnerMargin = 60; // margen del texto respecto al borde de la página

    private static readonly Lazy<LogoImage?> Logo = new(LoadLogo);

    public Task<byte[]> GenerateAsync(ReceiptPdfModel model, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var content = new StringBuilder();
        var logo = Logo.Value;

        // 1) Marco de la página (rectángulo con doble línea, similar al comprobante de referencia).
        DrawFrame(content);

        // 2) Logo centrado en la parte superior, dentro del marco.
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

        // 3) Encabezado institucional (centrado).
        var cursorY = headerTop;
        cursorY = CenteredText(content, model.InstitutionName.ToUpperInvariant(), "F2", 14, cursorY);
        cursorY -= 18;
        cursorY = CenteredText(content, "Comprobante interno de pago", "F1", 10, cursorY);
        cursorY -= 34;

        // 4) Título subrayado y centrado.
        const string title = "Comprobante de Pago";
        var titleWidth = MeasureText(title, 15, bold: true);
        var titleX = (PageWidth - titleWidth) / 2;
        CenteredText(content, title, "F2", 15, cursorY);
        // Subrayado.
        content.Append(CultureInfo.InvariantCulture,
            $"{F(titleX)} {F(cursorY - 4)} m {F(titleX + titleWidth)} {F(cursorY - 4)} l 0.6 w S\n");
        cursorY -= 40;

        // 5) Cuerpo: párrafo con los datos del pago (justificado a izquierda, con salto de línea por ancho).
        var issued = model.IssuedAt.ToString("dd 'de' MMMM 'de' yyyy 'a las' HH:mm 'UTC'",
            new CultureInfo("es-AR"));
        var amount = $"{model.Currency} {model.Amount.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
        var body =
            $"Se deja constancia que {model.StudentName} con DNI {model.Dni} ha efectuado un pago por " +
            $"un monto total de {amount}, abonado mediante {model.PaymentMethod}. " +
            $"El presente comprobante se emite bajo el número {model.ReceiptNumber} el {issued}. " +
            "Este documento acredita la registración del pago en el sistema de gestión del instituto.";

        var textWidth = PageWidth - (2 * InnerMargin);
        cursorY = WrappedText(content, body, "F1", 10.5, InnerMargin, cursorY, textWidth, 15);
        cursorY -= 22;

        // 6) Tabla de conceptos imputados.
        cursorY = LeftText(content, "Conceptos imputados", "F2", 11, InnerMargin, cursorY);
        cursorY -= 6;
        content.Append(CultureInfo.InvariantCulture,
            $"{F(InnerMargin)} {F(cursorY)} m {F(PageWidth - InnerMargin)} {F(cursorY)} l 0.5 w S\n");
        cursorY -= 16;

        var amountColumnX = PageWidth - InnerMargin - 110;
        foreach (var item in model.Items.Take(20))
        {
            var label = Trim($"{item.ConceptCode} - {item.ConceptName}", 70);
            LeftText(content, label, "F1", 10, InnerMargin, cursorY);
            var itemAmount = $"{model.Currency} {item.Amount.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
            LeftText(content, itemAmount, "F1", 10, amountColumnX, cursorY);
            cursorY -= 15;
        }

        cursorY -= 2;
        content.Append(CultureInfo.InvariantCulture,
            $"{F(InnerMargin)} {F(cursorY)} m {F(PageWidth - InnerMargin)} {F(cursorY)} l 0.5 w S\n");
        cursorY -= 16;
        LeftText(content, "TOTAL", "F2", 10.5, InnerMargin, cursorY);
        LeftText(content, amount, "F2", 10.5, amountColumnX, cursorY);
        cursorY -= 44;

        // 7) Código de validación.
        var validationCode = BuildValidationCode(model);
        CenteredText(content, $"CÓDIGO DE VALIDACIÓN: {validationCode}", "F2", 10, cursorY);
        cursorY -= 26;

        // 8) Datos del operador y pie.
        LeftText(content, $"Operador: {model.OperatorName} (ID {model.OperatorUserId})", "F1", 9, InnerMargin, cursorY);
        cursorY -= 14;
        LeftText(content, $"Emitido por: {model.InstitutionName}", "F1", 9, InnerMargin, cursorY);

        // 9) Pie de página (leyenda no fiscal).
        var footerY = Margin + 40;
        content.Append(CultureInfo.InvariantCulture,
            $"{F(InnerMargin)} {F(footerY + 16)} m {F(PageWidth - InnerMargin)} {F(footerY + 16)} l 0.5 w S\n");
        CenteredText(content, model.NonFiscalLegend, "F2", 8.5, footerY);
        CenteredText(content, "Este comprobante puede ser validado ante la administración del instituto citando el código de validación.",
            "F1", 8, footerY - 14);

        return Task.FromResult(BuildPdf(content.ToString(), logo, validationCode));
    }

    private static void DrawFrame(StringBuilder content)
    {
        // Borde exterior.
        content.Append(CultureInfo.InvariantCulture,
            $"0.15 0.22 0.29 RG\n1.5 w\n{F(Margin)} {F(Margin)} {F(PageWidth - 2 * Margin)} {F(PageHeight - 2 * Margin)} re S\n");
        // Borde interior fino.
        var inset = Margin + 6;
        content.Append(CultureInfo.InvariantCulture,
            $"0.5 w\n{F(inset)} {F(inset)} {F(PageWidth - 2 * inset)} {F(PageHeight - 2 * inset)} re S\n");
        // Restablecer color de trazo a negro para el resto.
        content.Append("0 0 0 RG\n");
    }

    private static double CenteredText(StringBuilder content, string text, string font, double size, double y)
    {
        var ascii = SanitizePreservingLatin(text);
        var width = MeasureText(text, size, bold: font == "F2");
        var x = (PageWidth - width) / 2;
        Emit(content, ascii, font, size, x, y);
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

        // Recursos: dos fuentes y (opcionalmente) la imagen del logo.
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

        // Objeto 7: imagen del logo (si está disponible), escrito como bytes crudos.
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
            $"/Info << /Title (Comprobante {Escape(validationCode)}) >> >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string BuildValidationCode(ReceiptPdfModel model)
    {
        // Código determinista y legible derivado del número de comprobante, DNI y fecha.
        var seed = $"{model.ReceiptNumber}|{model.Dni}|{model.IssuedAt:yyyyMMddHHmm}";
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var digits = new StringBuilder();
        foreach (var b in hash)
        {
            digits.Append((b % 10).ToString(CultureInfo.InvariantCulture));
            if (digits.Length >= 14) break;
        }
        return digits.ToString();
    }

    // Anchos aproximados de Helvetica (unidades/1000) para medir texto y centrar/justificar.
    // Usamos anchos promedio por rango para no incrustar toda la tabla AFM.
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

    // Conserva los caracteres Latin-1 (acentos, ñ) que WinAnsiEncoding sí puede representar,
    // reemplazando los que caen fuera del rango por '?'.
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

            // Header: width(4) height(4) big-endian, luego RGB comprimido con zlib.
            var width = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
            var height = (bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7];
            if (width <= 0 || height <= 0) return null;

            var compressed = new byte[bytes.Length - 8];
            Array.Copy(bytes, 8, compressed, 0, compressed.Length);
            return new LogoImage(width, height, compressed);
        }
        catch
        {
            // Si el logo no se puede cargar, el comprobante se genera igual sin imagen.
            return null;
        }
    }

    private sealed record LogoImage(int Width, int Height, byte[] CompressedRgb);
}
