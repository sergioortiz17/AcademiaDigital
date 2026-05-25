using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.User;

public class ChangePasswordUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    public async Task ExecuteAsync(long userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        if (!passwordHasher.Verify(currentPassword, user.Password))
            throw new InvalidCurrentPasswordException();

        await userRepository.UpdatePasswordAsync(userId, passwordHasher.Hash(newPassword), ct);
    }
}
