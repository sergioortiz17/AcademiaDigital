namespace AcademiaDigital.Domain.Services;

/// <summary>
/// Reglas para los archivos que sube un usuario: PDF, JPEG o PNG de hasta 10 MB. El tipo se
/// comprueba por el contenido (firma del archivo), no solo por lo que declara el navegador.
/// </summary>
public static class UploadedDocumentPolicy
{
    public const long MaximumFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>Devuelve el content type real del archivo o lanza ArgumentException.</summary>
    public static string Validate(string? fileName, ReadOnlySpan<byte> content)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName.Length > 200)
            throw new ArgumentException("El nombre del archivo no es válido.");
        if (content.Length == 0 || content.Length > MaximumFileSizeBytes)
            throw new ArgumentException("El archivo debe pesar entre 1 byte y 10 MB.");
        if (content.StartsWith("%PDF"u8)) return "application/pdf";
        if (content.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (content.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return "image/jpeg";
        throw new ArgumentException("El archivo debe ser PDF, JPG o PNG.");
    }

    /// <summary>Nombre seguro para la clave de almacenamiento (sin rutas ni caracteres raros).</summary>
    public static string SafeFileName(string fileName)
    {
        var chars = fileName.Trim().Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray();
        var safe = new string(chars).Trim('.');
        return string.IsNullOrEmpty(safe) ? "documento" : safe[..Math.Min(safe.Length, 100)];
    }
}
