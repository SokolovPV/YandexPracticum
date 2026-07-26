using EventFlow.Entities.Decorator;
using EventFlow.Users.Application.Interfaces;
using EventFlow.Users.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EventFlow.Users.Infrastructure.Repositories;

/// <summary>
/// Декоратор для репозитория пользователей с обработкой ошибок PostgreSQL
/// </summary>
public class DatabaseErrorHandlingUserRepositoryDecorator
    : BaseDatabaseErrorHandlingRepositoryDecorator<IUserRepository>, IUserRepository
{
    public DatabaseErrorHandlingUserRepositoryDecorator(
    IUserRepository inner,
    ILogger<DatabaseErrorHandlingUserRepositoryDecorator> logger)
    : base(inner, logger)
    {
    }
    public async Task AddAsync(User user, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _inner.AddAsync(user, ct),
            nameof(AddAsync),
            $"UserId: {user.Id}, Login: {user.Login}");
    }

    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetUserByLoginAsync(login, ct),
            nameof(GetUserByLoginAsync),
            $"Login: {login}");
    }

    public async Task<bool> ExistsAsync(string login, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.ExistsAsync(login, ct),
            nameof(ExistsAsync),
            $"Login: {login}");
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetUserByIdAsync(userId, ct),
            nameof(GetUserByIdAsync),
            $"UserId: {userId}");
    }
}