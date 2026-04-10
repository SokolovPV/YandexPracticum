using System.Collections.Concurrent;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;
namespace EventsApi.Infrastructure.Repository;
/// <summary>
/// Репозиторий для in-memory коллекции Booking
/// </summary>
public class InMemoryBookingRepository : IBookingRepository
{
    private static readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

    /// <inheritdoc/>
    public async Task AddAsync(Booking booking, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Task.FromResult(_bookings.TryAdd(booking.Id, booking));
    }
    /// <inheritdoc/>
    public Task<bool> DeleteAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(_bookings.TryRemove(bookingId, out _));
    }
    /// <inheritdoc/>
    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _bookings.TryGetValue(bookingId, out var booking);
        return Task.FromResult(booking);
    }
    /// <inheritdoc/>
    public Task<List<Booking>> ListAsync(Func<Booking, bool> query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (query != null)
            return Task.FromResult(_bookings.Values.Where(query).ToList());

        return Task.FromResult(_bookings.Values.ToList());
    }
    /// <inheritdoc/>
    public Task UpdateAsync(Booking booking, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _bookings[booking.Id] = booking;
        return Task.CompletedTask;
    }
}
