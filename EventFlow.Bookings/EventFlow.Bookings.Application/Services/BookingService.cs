
using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Application.Options;
using EventFlow.Bookings.Domain.Entities;
using EventFlow.Bookings.Domain.Enums;
using EventFlow.Bookings.Domain.Exceptions;
using EventFlow.Entities.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventFlow.Bookings.Application.Services;
/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IOptions<BookingSettings> bookingSettings,
    ILogger<BookingService> logger) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly BookingSettings _bookingSettings = bookingSettings.Value;

    /// <inheritdoc/>
    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, RoleType role, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Отмена брони: {BookingId}", bookingId);
        await _semaphore.WaitAsync(ct);
        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
            if (booking is null)
                throw new KeyNotExistException(bookingId.ToString(), nameof(Booking));
            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Rejected)
                throw new InvalidOperationException($"Бронирование '{bookingId}' уже отменено ранее");
            if (booking.UserId != userId && role != RoleType.Admin)
                throw new AccessDeniedException(userId.ToString(), nameof(CancelBookingAsync));

            booking.Cancel();
            await bookingRepository.UpdateAsync(booking, ct);

            logger.LogInformation("Бронирование ID: {bookingId} успешно отменено.", bookingId);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Создание новой брони для события: {Event}", eventId);
        await _semaphore.WaitAsync(ct);
        try
        {
            // var _event = await eventRepository.GetByIdAsync(eventId, ct);
            // if (_event is null)
            // {
            //     logger.LogError("Идентификатор мероприятия {Id} не найден.", eventId);
            //     throw new KeyNotExistException(eventId, ConstantValues.key_not_found_exception);
            // }
            // if (!_event.TryReserveSeats())
            //     throw new NoAvailableSeatsException(_event.Id);

            // if (_event.StartAt < DateTime.UtcNow)
            // {
            //     logger.LogError("Событие уже началось и недоступно для бронирования");
            //     throw new EventAlreadyStartedException(eventId.ToString(), DateTime.UtcNow);
            // }

            var userBookings = await bookingRepository.ListAsync(q => q.UserId == userId
                                    && q.Status != BookingStatus.Rejected && q.Status != BookingStatus.Cancelled, ct);
            if (userBookings != null && userBookings.Count >= _bookingSettings.MaxUserBookings)
                throw new BookingLimitExceededException(eventId.ToString(), userId.ToString(), userBookings.Count, _bookingSettings.MaxUserBookings);

            var newBooking = Booking.Create(eventId, userId);
            await bookingRepository.AddAsync(newBooking, ct);
            logger.LogInformation("Бронирование создано. ID: {Id} ", newBooking.Id);
            return newBooking;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    /// <inheritdoc/>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Получение бронирования : {bookingId}", bookingId);
        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (booking is null)
            throw new KeyNotExistException(bookingId.ToString(), nameof(Booking));
        return booking;
    }
    /// <inheritdoc/>ы
    public async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
    {
        try
        {
            var saved = await bookingRepository.ConfirmAsync(booking.Id, ct);
            if (!saved)
            {
                logger.LogWarning("Не удалось подтвердить бронирование {BookingId}.", booking.Id);
                return;
            }
            logger.LogInformation("Бронь {Id} подтверждена", booking.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Ошибка при обработке бронирования {Id}", booking.Id);

            var rejected = await bookingRepository.RejectAsync(booking.Id, ct);

            if (rejected)
                logger.LogInformation("Бронь {Id} отклонена", booking.Id);

            throw;
        }
    }
}