
using System.Text.Json;
using EventFlow.Bookings.Application.Interfaces;
using EventFlow.Bookings.Application.Options;
using EventFlow.Bookings.Domain.Entities;
using EventFlow.Bookings.Domain.Enums;
using EventFlow.Bookings.Domain.Exceptions;
using EventFlow.Entities.Brokers;
using EventFlow.Entities.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventFlow.Bookings.Application.Services;
/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IBookingConfirmedProducer bookingConfirmedProducer,
    IOptions<BookingOptions> bookingSettings,
    ILogger<BookingService> logger) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly BookingOptions _bookingSettings = bookingSettings.Value;

    /// <inheritdoc/>
    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, RoleType role, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Отмена бронирования: {bookingId}", bookingId);
        await _semaphore.WaitAsync(ct);
        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
            if (booking is null)
            {
                logger.LogWarning("Бронирование с идентификатором {bookingId} не найдено.", bookingId);
                throw new KeyNotExistException(bookingId.ToString(), nameof(Booking));
            }
            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Rejected)
            {
                logger.LogInformation("Бронирование с идентификатором '{bookingId}' уже отменено ранее.", bookingId);
                throw new InvalidOperationException($"Бронирование '{bookingId}' уже отменено ранее.");
            }
            if (booking.UserId != userId && role != RoleType.Admin)
            {
                logger.LogWarning("Пользователь {userId} не имеет прав на выполнение действия {action}.", userId, nameof(CancelBookingAsync));
                throw new AccessDeniedException(userId.ToString(), nameof(CancelBookingAsync));
            }

            booking.Cancel();
            await bookingRepository.UpdateAsync(booking, ct);

            logger.LogInformation("Бронирование с идентификатором {bookingId} успешно отменено.", bookingId);
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
        logger.LogInformation("Создание нового бронирования для события: {eventId}", eventId);
        await _semaphore.WaitAsync(ct);
        try
        {
            var userBookings = await bookingRepository.ListAsync(q => q.UserId == userId
                                    && q.Status != BookingStatus.Rejected && q.Status != BookingStatus.Cancelled, ct);
            if (userBookings != null && userBookings.Count >= _bookingSettings.MaxUserBookings)
            {
                logger.LogWarning("Превышен лимит активных бронирований пользователя {userId}. "
                + "Текущее количество: {currentCount}, максимум: {maxLimit}", userId, userBookings.Count, _bookingSettings.MaxUserBookings);
                throw new BookingLimitExceededException(userId.ToString(), userBookings.Count, _bookingSettings.MaxUserBookings);
            }

            var newBooking = Booking.Create(eventId, userId);
            await bookingRepository.AddAsync(newBooking, ct);
            logger.LogInformation("Бронирование создано. Идентификатор бронирования: {bookingId} ", newBooking.Id);
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
        {
            logger.LogWarning("Бронирование с идентификатором {bookingId} не найдено.", bookingId);
            throw new KeyNotExistException(bookingId.ToString(), nameof(Booking));
        }
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
                logger.LogWarning("Не удалось подтвердить бронирование {bookingId}.", booking.Id);
                return;
            }
            logger.LogInformation("Бронирование с идентификаторм {bookingId} подтверждено", booking.Id);

            var confirmedMessage = new BookingConfirmed(
                           Guid.NewGuid(),
                           booking.Id,
                           booking.EventId,
                           booking.UserId,
                           DateTime.UtcNow);

            await bookingConfirmedProducer.PublishAsync(
                TopicNames.BookingConfirmed,
                booking.EventId.ToString(),
                JsonSerializer.Serialize(confirmedMessage),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Ошибка при обработке бронирования {bookingId}", booking.Id);

            var rejected = await bookingRepository.RejectAsync(booking.Id, ct);

            if (rejected)
                logger.LogInformation("Бронирование {bookingId} отклонено", booking.Id);

            throw;
        }
    }
}