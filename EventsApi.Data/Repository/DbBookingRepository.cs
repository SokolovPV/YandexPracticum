using System.Linq.Expressions;
using EventsApi.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsApi.DataAccess;
/// <summary>
/// Репозиторий для коллекции Booking в БД PostreSQL
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
            return await appDbContext.Bookings.Where(query).ToListAsync(ct);

        return await appDbContext.Bookings.ToListAsync(ct);
    }
    /// <inheritdoc/>
    public async Task UpdateAsync(Booking booking, CancellationToken ct)
    {
        var entity = await appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id, ct);
        if (entity != null)
        {
            entity.Status = booking.Status;
            entity.ProcessedAt = booking.ProcessedAt;
            await appDbContext.SaveChangesAsync(ct);
        }
    }
}
