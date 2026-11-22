using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.User;

public class UpdateUserUseCase(IUserRepository userRepository)
{
    public async Task<UpdateUserResult> ExecuteAsync(UpdateUserCommand cmd, CancellationToken ct = default)
    {
        if (cmd.CurrentUserId != cmd.TargetUserId && !cmd.IsSuperuser)
            throw new UnauthorizedUserUpdateException();

        var user = await userRepository.FindByIdAsync(cmd.TargetUserId, ct)
            ?? throw new UnauthorizedUserUpdateException();

        if (cmd.Email is not null) user.Email = cmd.Email.Trim();
        if (cmd.Username is not null) user.Username = cmd.Username.Trim();
        if (cmd.LastName is not null) user.LastName = cmd.LastName.Trim();
        if (cmd.Dni is not null) user.Dni = cmd.Dni.Trim();
        if (cmd.Gender is not null) user.Gender = cmd.Gender.Trim();
        if (cmd.Cuil is not null) user.Cuil = ContactFormatPolicy.NormalizeCuil(cmd.Cuil);
        if (cmd.BirthDate is not null) user.BirthDate = cmd.BirthDate;
        if (cmd.PhoneCode is not null || cmd.Phone is not null)
        {
            var (areaCode, number) = ContactFormatPolicy.NormalizePhone(
                cmd.PhoneCode ?? user.PhoneCode, cmd.Phone ?? user.Phone);
            user.PhoneCode = areaCode;
            user.Phone = number;
        }

        var updated = await userRepository.UpdateAsync(user, ct);

        return new UpdateUserResult(
            Id: updated.Id,
            Username: updated.Username,
            LastName: updated.LastName,
            Email: updated.Email,
            Dni: updated.Dni,
            Gender: updated.Gender,
            Cuil: updated.Cuil,
            BirthDate: updated.BirthDate,
            PhoneCode: updated.PhoneCode,
            Phone: updated.Phone,
            Date: updated.DateJoined);
    }
}

public record UpdateUserCommand(
    long TargetUserId,
    long CurrentUserId,
    bool IsSuperuser,
    string? Email = null,
    string? Username = null,
    string? LastName = null,
    string? Dni = null,
    string? Gender = null,
    string? Cuil = null,
    DateTime? BirthDate = null,
    string? PhoneCode = null,
    string? Phone = null);

public record UpdateUserResult(
    long Id,
    string Username,
    string LastName,
    string Email,
    string? Dni,
    string? Gender,
    string? Cuil,
    DateTime? BirthDate,
    string? PhoneCode,
    string? Phone,
    DateTime Date);
