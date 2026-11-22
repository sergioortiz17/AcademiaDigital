using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Admin;

/// <summary>
/// Cambia el rol de una cuenta y deja el perfil acorde al rol nuevo: sin perfil, la cuenta ve
/// las pantallas del rol pero falla al usarlas (por ejemplo, "Perfil de docente no encontrado").
/// </summary>
public class UpdateUserRoleUseCase(
    IUserRepository userRepository,
    ITeacherRepository teacherRepository,
    IStudentRepository studentRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<UserSummary> ExecuteAsync(long requestingUserId, long targetUserId, UserRole newRole, CancellationToken ct = default)
    {
        if (requestingUserId == targetUserId)
            throw new ForbiddenException("No puedes cambiar tu propio rol.");

        // Un alumno necesita carrera y plan: no se puede inventar el perfil al cambiar el rol.
        if (newRole == UserRole.Alumno && await studentRepository.FindByUserIdAsync(targetUserId, ct) is null)
            throw new InvalidOperationException(
                "La cuenta no tiene perfil de alumno. Para que sea alumno, dela de alta con su carrera desde Aspirantes.");

        var updated = await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var user = await userRepository.UpdateRoleAsync(targetUserId, newRole, transactionCt);
            if (newRole == UserRole.Profesor && await teacherRepository.FindByUserIdAsync(user.Id, transactionCt) is null)
            {
                await teacherRepository.CreateAsync(new Teacher
                {
                    UserId = user.Id,
                    EmployeeNumber = $"DOC-{user.Id:D5}",
                    HireDate = DateTime.UtcNow.Date,
                    IsActive = true
                }, transactionCt);
            }
            return user;
        }, ct);

        return new UserSummary(
            Id: updated.Id,
            Username: updated.Username,
            Email: updated.Email,
            Dni: updated.Dni,
            Role: updated.Role,
            IsActive: updated.IsActive,
            DateJoined: updated.DateJoined);
    }
}
