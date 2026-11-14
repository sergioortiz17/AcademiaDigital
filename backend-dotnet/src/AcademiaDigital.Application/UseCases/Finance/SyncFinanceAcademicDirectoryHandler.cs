using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Application.UseCases.Finance;

public sealed class SyncFinanceAcademicDirectoryHandler(
    IAcademicDirectoryReader directoryReader,
    IFinanceClient financeClient)
{
    public async Task<bool> Handle(long actorUserId, UserRole actorRole, CancellationToken ct = default)
    {
        var students = await directoryReader.ReadAsync(ct);
        return students.Count == 0
            || await financeClient.TrySyncAcademicDirectoryAsync(students, actorUserId, actorRole, ct);
    }
}
