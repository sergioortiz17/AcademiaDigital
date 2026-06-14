using AcademiaDigital.Application.Dtos;

namespace AcademiaDigital.Application.Interfaces;

public interface ISchedulePdfExporter
{
    Task<byte[]> ExportAsync(ScheduleGridDto schedule, CancellationToken ct = default);
}
