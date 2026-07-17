using System.Linq.Expressions;
using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Domain.Entities;
using EventFlow.Events.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Events.Infrastructure.Repositories;
/// <summary>
/// Репозиторий для коллекции Event в БД PostreSQL
/// </summary>
public class DbEventRepository(AppDbContext appDbContext) : IEventRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(Event _event, CancellationToken ct)
    {
        await appDbContext.Events.AddAsync(_event, ct);
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
        await appDbContext.SaveChangesAsync(ct);
        return true;
    }
    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await appDbContext.Events.FirstOrDefaultAsync(q => q.Id == id, ct);
    }

    /// <inheritdoc/>
    public async Task<List<Event>> GetTop10EventAsync(CancellationToken ct)
    {
        return await appDbContext
        .Events
        .AsNoTracking()
        .Where(e => e.AvailableSeats < e.TotalSeats) // события с забронированными местами
        .OrderByDescending(e => (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats) // сортируем по % забронированных мест
        .Take(10)
        .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<Event>> ListAsync(Expression<Func<Event, bool>> query, int page, int pageSize, CancellationToken ct)
    {

        return await appDbContext.Events.Where(query)
                                        .OrderBy(c => c.Title)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync(ct);
    }


    /// <inheritdoc/>
    public async Task UpdateAsync(Event _event, CancellationToken ct)
    {
        appDbContext.Events.Update(_event);
        await appDbContext.SaveChangesAsync(ct);
    }

}