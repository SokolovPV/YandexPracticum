
using EventFlow.Entities.Enums;
using EventFlow.Users.Application.Interfaces;
using EventFlow.Users.Domain.Entities;

namespace EventFlow.Users.Application.Services;
/// <summary>
/// Сервис для работы с пользователями
/// </summary>
public class AuthenticationService(IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator, IUserRepository userRepository) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<string?> LoginAsync(string login, string password, CancellationToken ct)
    {
        var user = await userRepository.GetUserByLoginAsync(login, ct);
        if (user is null)
            return null;

        var isPasswordValid = passwordHasher.VerifyHashedPassword(password, user.PasswordHash);
        return !isPasswordValid
            ? null
            : tokenGenerator.GenerateToken(user, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> RegisterUserAsync(string login, string password, RoleType role, CancellationToken ct)
    {
        if (await userRepository.ExistsAsync(login, ct))
            return false;

        var hash = passwordHasher.HashPassword(password);
        var user = User.Create(login, hash, role);

        await userRepository.AddAsync(user, ct);
        return true;
    }
}