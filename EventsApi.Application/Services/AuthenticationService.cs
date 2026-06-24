using EventsApi.Application.Interfaces;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;
/// <summary>
/// Сервис для работы с пользователями
/// </summary>
public class AuthenticationService(IPasswordHasher passwordHasher, IUserRepository userRepository, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public Task<string?> LoginUserAsync(string login, string password, CancellationToken ct)
    {
        throw new NotImplementedException();
    }


    public async Task<bool> RegisterUserAsync(string login, string password, RoleType role,  CancellationToken ct)
    {
		    if (await userRepository.ExistsAsync(login, ct))
			    return false;

		    var hash = passwordHasher.HashPassword(password);
		    var user = User.Create(login, hash, role);

		    await userRepository.AddAsync(user, ct);
		    return true;
    }
}