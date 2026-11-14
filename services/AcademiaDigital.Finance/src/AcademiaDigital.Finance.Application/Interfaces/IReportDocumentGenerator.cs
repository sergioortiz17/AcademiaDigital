namespace AcademiaDigital.Finance.Application.Interfaces;

public sealed record GeneratedReport(byte[] Content, string ContentType, string FileName);

public interface IReportDocumentGenerator
{
    GeneratedReport GeneratePdf(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string fileName);
    GeneratedReport GenerateExcel(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, string fileName);
}
