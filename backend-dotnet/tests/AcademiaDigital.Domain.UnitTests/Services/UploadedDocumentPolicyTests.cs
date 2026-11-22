using System.Text;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class UploadedDocumentPolicyTests
{
    [Fact]
    public void Detects_the_real_type_by_content()
    {
        Assert.Equal("application/pdf", UploadedDocumentPolicy.Validate("analitico.pdf", Encoding.ASCII.GetBytes("%PDF-1.7 ...")));
        Assert.Equal("image/png", UploadedDocumentPolicy.Validate("foto.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1 }));
        Assert.Equal("image/jpeg", UploadedDocumentPolicy.Validate("foto.jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
    }

    [Fact]
    public void Rejects_other_files_even_with_an_allowed_extension()
    {
        Assert.Throws<ArgumentException>(() => UploadedDocumentPolicy.Validate("virus.pdf", Encoding.ASCII.GetBytes("MZ....")));
        Assert.Throws<ArgumentException>(() => UploadedDocumentPolicy.Validate("vacio.pdf", []));
        Assert.Throws<ArgumentException>(() => UploadedDocumentPolicy.Validate("../x.pdf", Encoding.ASCII.GetBytes("%PDF")));
    }

    [Fact]
    public void Builds_a_safe_storage_name()
        => Assert.Equal("Anal_tico_secundario.pdf", UploadedDocumentPolicy.SafeFileName("Analítico secundario.pdf"));
}
