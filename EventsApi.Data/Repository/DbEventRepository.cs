using System.Collections.Concurrent;
using System.Linq.Expressions;
using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.DataAccess;
/// <summary>
/// Репозиторий для in-memory коллекции Event
/// </summary>
public class DbEventRepository(AppDbContext appDbContext) : IEventRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(Event _event, CancellationToken ct)
    {
        await appDbContext.AddAsync(_event, ct);
        await appDbContext.SaveChangesAsync(ct);
    }
    /// <inheritdoc/>
    public async Task<int> CountAsync(Expression<Func<Event, bool>> query, CancellationToken ct)
    {
        return await appDbContext.Events.CountAsync(query, ct);
    }
    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var _event = await appDbContext.Events.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (_event == null)
            return false;

        appDbContext.Events.Remove(_event);
        await appDbContext.SaveChangesAsync();
        return true;
    }
    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await appDbContext.Events.FirstOrDefaultAsync(q => q.Id == id, ct);
    }
    /// <inheritdoc/>
    public async Task<List<Event>> ListAsync(Expression<Func<Event, bool>> query, int page, int pageSize, CancellationToken ct)
    {

        return await appDbContext.Events.Where(query)
                                        .OrderBy(c => c.Title)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();
    }
    /// <inheritdoc/>
    public async Task UpdateAsync(Event _event, CancellationToken ct)
    {
        await appDbContext.SaveChangesAsync();
    }
}