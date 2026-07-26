using System.Linq.Expressions;
using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Domain.Entities;
using EventFlow.Entities.Decorator;
using Microsoft.Extensions.Logging;

namespace EventFlow.Bookings.Infrastructure.Repositories;

/// <summary>
/// Декоратор для репозитория с обработкой ошибок PostgreSQL
/// </summary>

public class DatabaseErrorHandlingBookingRepositoryDecorator
    : BaseDatabaseErrorHandlingRepositoryDecorator<IBookingRepository>, IBookingRepository
{
    public DatabaseErrorHandlingBookingRepositoryDecorator(
    IBookingRepository inner,
    ILogger<DatabaseErrorHandlingBookingRepositoryDecorator> logger)
    : base(inner, logger)
    {
    }


    public async Task AddAsync(Booking booking, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _inner.AddAsync(booking, ct),
            nameof(AddAsync),
            $"BookingId: {booking.Id}");
    }

    public async Task<bool> DeleteAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.DeleteAsync(bookingId, ct),
            nameof(DeleteAsync),
            $"BookingId: {bookingId}");
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.GetByIdAsync(bookingId, ct),
            nameof(GetByIdAsync),
            $"BookingId: {bookingId}");
    }

    public async Task<List<Booking>> ListAsync(Expression<Func<Booking, bool>>? query, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.ListAsync(query, ct),
            nameof(ListAsync),
            $"HasQuery: {query != null}");
    }

    public async Task UpdateAsync(Booking booking, CancellationToken ct)
    {
        await ExecuteWithErrorHandlingAsync(
            () => _inner.UpdateAsync(booking, ct),
            nameof(UpdateAsync),
            $"BookingId: {booking.Id}");
    }

    public async Task<bool> ConfirmAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.ConfirmAsync(bookingId, ct),
            nameof(ConfirmAsync),
            $"BookingId: {bookingId}");
    }

    public async Task<bool> RejectAsync(Guid bookingId, CancellationToken ct)
    {
        return await ExecuteWithErrorHandlingAsync(
            () => _inner.RejectAsync(bookingId, ct),
            nameof(RejectAsync),
            $"BookingId: {bookingId}");
    }
}