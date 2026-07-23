using System.Linq.Expressions;
using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventFlow.Events.Infrastructure.Repositories;

/// <summary>
/// Декоратор для репозитория событий с обработкой ошибок PostgreSQL
/// </summary>
public class DatabaseErrorHandlingEventRepositoryDecorator : IEventRepository
{
    private readonly IEventRepository _inner;
    private readonly ILogger<DatabaseErrorHandlingEventRepositoryDecorator> _logger;

    public DatabaseErrorHandlingEventRepositoryDecorator(
        IEventRepository inner,
        ILogger<DatabaseErrorHandlingEventRepositoryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Event _event, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _inner.AddAsync(_event, ct),
            nameof(AddAsync),
            $"EventId: {_event.Id}");
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(Expression<Func<Event, bool>> query, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.CountAsync(query, ct),
            nameof(CountAsync),
            "Count query");
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.DeleteAsync(id, ct),
            nameof(DeleteAsync),
            $"EventId: {id}");
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetByIdAsync(id, ct),
            nameof(GetByIdAsync),
            $"EventId: {id}");
    }

    /// <inheritdoc/>
    public async Task<List<Event>> GetTop10EventAsync(CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetTop10EventAsync(ct),
            nameof(GetTop10EventAsync),
            "Top 10 events");
    }

    /// <inheritdoc/>
    public async Task<List<Event>> ListAsync(Expression<Func<Event, bool>> query, int page, int pageSize, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.ListAsync(query, page, pageSize, ct),
            nameof(ListAsync),
            $"Page: {page}, PageSize: {pageSize}");
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Event _event, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _inner.UpdateAsync(_event, ct),
            nameof(UpdateAsync),
            $"EventId: {_event.Id}");
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