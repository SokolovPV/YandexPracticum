using EventFlow.Users.Application.Interfaces;
using EventFlow.Users.Domain.Entities;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventFlow.Users.Infrastructure.Repositories;

/// <summary>
/// Декоратор для репозитория пользователей с обработкой ошибок PostgreSQL
/// </summary>
public class DatabaseErrorHandlingUserRepositoryDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly ILogger<DatabaseErrorHandlingUserRepositoryDecorator> _logger;

    public DatabaseErrorHandlingUserRepositoryDecorator(
        IUserRepository inner,
        ILogger<DatabaseErrorHandlingUserRepositoryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task AddAsync(User user, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _inner.AddAsync(user, ct),
            nameof(AddAsync),
            $"UserId: {user.Id}, Login: {user.Login}");
    }

    /// <inheritdoc/>
    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetUserByLoginAsync(login, ct),
            nameof(GetUserByLoginAsync),
            $"Login: {login}");
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string login, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.ExistsAsync(login, ct),
            nameof(ExistsAsync),
            $"Login: {login}");
    }

    /// <inheritdoc/>
    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetUserByIdAsync(userId, ct),
            nameof(GetUserByIdAsync),
            $"UserId: {userId}");
    }

    #region Private Methods

    private async Task<T> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string methodName,
        string context)
    {
        try
        {
            return await operation();
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "Ошибка PostgreSQL в методе {MethodName}. {Context}", methodName, context);
            throw;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Ошибка подключения к PostgreSQL в методе {MethodName}. {Context}", methodName, context);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка в методе {MethodName}. {Context}", methodName, context);
            throw;
        }
    }

    private async Task ExecuteWithErrorHandlingAsync(
        Func<Task> operation,
        string methodName,
        string context)
    {
        try
        {
            await operation();
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "Ошибка PostgreSQL в методе {MethodName}. {Context}", methodName, context);
            throw;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Ошибка подключения к PostgreSQL в методе {MethodName}. {Context}", methodName, context);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка в методе {MethodName}. {Context}", methodName, context);
            throw;
        }
    }

    #endregion
}