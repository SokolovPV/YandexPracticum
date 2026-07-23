using System.Linq.Expressions;
using EventFlow.Entities.Decorator;
using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventFlow.Events.Infrastructure.Repositories;

/// <summary>
/// Декоратор для репозитория событий с обработкой ошибок PostgreSQL
/// </summary>
public class DatabaseErrorHandlingEventRepositoryDecorator
    : BaseDatabaseErrorHandlingRepositoryDecorator<IEventRepository>, IEventRepository
{
    public DatabaseErrorHandlingEventRepositoryDecorator(
    IEventRepository inner,
    ILogger<DatabaseErrorHandlingEventRepositoryDecorator> logger)
    : base(inner, logger)
    {
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
}