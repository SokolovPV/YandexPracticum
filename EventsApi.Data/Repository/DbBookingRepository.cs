using System.Linq.Expressions;
using EventsApi.DataAccess.Repository.Interfaces;
using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.DataAccess.Repository;
/// <summary>
/// Репозиторий для in-memory коллекции Booking
/// </summary>
public class DbBookingRepository(AppDbContext appDbContext) : IBookingRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(Booking booking, CancellationToken ct)
    {
        await appDbContext.Bookings.AddAsync(booking, ct);
        await appDbContext.SaveChangesAsync();
  }
    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid bookingId, CancellationToken ct)
    {
        var booking = await appDbContext.Bookings.FirstOrDefaultAsync(q => q.Id == bookingId, ct);
        if (booking == null)
            return false;

        appDbContext.Bookings.Remove(booking);
        await appDbContext.SaveChangesAsync();
        return true;
    }
    /// <inheritdoc/>
    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct)
    {
        return await appDbContext.Bookings.FirstOrDefaultAsync(q => q.Id == bookingId, ct);
    }
    /// <inheritdoc/>
    public async Task<List<Booking>> ListAsync(Expression<Func<Booking, bool>> query, CancellationToken ct)
    {
        if (query != null)
            return await appDbContext.Bookings.Where(query).ToListAsync();

        return await appDbContext.Bookings.ToListAsync();
    }
    /// <inheritdoc/>
    public async Task UpdateAsync(Booking booking, CancellationToken ct)
    {
        await appDbContext.SaveChangesAsync();
    }
}
