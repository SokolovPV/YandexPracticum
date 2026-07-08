using EventFlow.Users.Domain.Enums;

namespace EventFlow.Users.Application.Interfaces;
/// <summary>
/// Сервис для работы с пользователями
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    /// <param name="password">Пароль</param>
    /// <param name="role">Роль</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> RegisterUserAsync(string login, string password, RoleType role, CancellationToken ct);

    /// <summary>
    /// Вход пользователя
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    /// <param name="password">Пароль</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Токен</returns>
    Task<string?> LoginAsync(string login, string password, CancellationToken ct);
}