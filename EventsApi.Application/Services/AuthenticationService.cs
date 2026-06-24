using EventsApi.Application.Interfaces;
using EventsApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EventsApi.Application.Services;
/// <summary>
/// Сервис для работы с пользователями
/// </summary>
public class AuthenticationService(ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public Task<string?> LoginUserAsync(string login, string password)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RegisterUserAsync(string login, string password, string? role)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RegisterUserAsync(string login, string password, RoleType role)
    {
        throw new NotImplementedException();
    }
}