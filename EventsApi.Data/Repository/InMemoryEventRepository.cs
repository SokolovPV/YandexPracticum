using System.Collections.Concurrent;
using System.Linq.Expressions;
using EventsApi.DataAccess.Repository.Interfaces;
using EventsApi.Models.Domain;

namespace EventsApi.DataAccess.Repository;
/// <summary>
/// Репозиторий для in-memory коллекции Event
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private static readonly ConcurrentDictionary<Guid, Event> _events = new();
    /// <inheritdoc/>
    public async Task AddAsync(Event _event, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Task.FromResult(_events.TryAdd(_event.Id, _event));
    }
    /// <inheritdoc/>
    public async Task<int> CountAsync(Expression<Func<Event, bool>> query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return await Task.FromResult(_events.Values.Count(query.Compile()));
    }
    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return await Task.FromResult(_events.TryRemove(id, out _));
    }
    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _events.TryGetValue(id, out var _event);
        return await Task.FromResult(_event);
    }
    /// <inheritdoc/>
    public async Task<List<Event>> ListAsync(Expression<Func<Event, bool>> query, int page, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return await Task.FromResult(_events.Values.Where(query.Compile())
                                                    .OrderBy(c => c.Title)
                                                    .Skip((page - 1) * pageSize)
                                                    .Take(pageSize)
                                                    .ToList());
    }
    /// <inheritdoc/>
    public async Task UpdateAsync(Event _event, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _events[_event.Id] = _event;
        await Task.CompletedTask;
    }
}