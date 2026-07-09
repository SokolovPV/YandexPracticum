using EventsApi.Application.Interfaces;
using EventsApi.Application.Options;
using EventsApi.Domain.Entities;
using EventsApi.Domain.Enums;
using EventsApi.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace EventsApi.Application.Services;
/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(
    IEventRepository eventRepository,
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IOptions<BookingSettings> bookingSettings,
    ILogger<BookingService> logger) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly BookingSettings _bookingSettings = bookingSettings.Value;

    /// <inheritdoc/>
    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
         logger.LogInformation("Отмена брони: {BookingId}", bookingId);
        await _semaphore.WaitAsync(ct);
        try
        {
            var user = await userRepository.GetUserByIdAsync(userId, ct);
            if (user is null)
                throw new EntityNotFoundException(nameof(User), userId.ToString());
            var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
            if (booking is null)
                throw new EntityNotFoundException(nameof(Booking), bookingId.ToString());
            if (booking.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException($"Бронирование '{bookingId}' уже отменено ранее");
            if (booking.UserId != userId && user.Role != RoleType.Admin)
                throw new AccessDeniedException(user.Id.ToString(), nameof(CancelBookingAsync));
            var @event = await eventRepository.GetByIdAsync(booking.EventId, ct);
            if (@event is null)
                throw new EntityNotFoundException(nameof(Event), booking.EventId.ToString());

            booking.Cancel();
            @event.ReleaseSeats();
            await eventRepository.UpdateAsync(@event, ct);
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
            var _event = await eventRepository.GetByIdAsync(eventId, ct);
            if (_event is null)
            {
                logger.LogError("Идентификатор мероприятия {Id} не найден.", eventId);
                throw new KeyNotExistException(eventId, ConstantValues.key_not_found_exception);
            }
            if (!_event.TryReserveSeats())
                throw new NoAvailableSeatsException(_event.Id);

            if (_event.StartAt < DateTime.UtcNow)
            {
                logger.LogError("Событие уже началось и недоступно для бронирования");
                throw new EventAlreadyStartedException(eventId.ToString(), DateTime.UtcNow);
            }

            var userBookings = await bookingRepository.ListAsync(q => q.UserId == userId 
                                    && q.Status != BookingStatus.Rejected && q.Status != BookingStatus.Cancelled, ct);
            if (userBookings != null && userBookings.Count >= _bookingSettings.MaxUserBookings)
                throw new BookingLimitExceededException(eventId.ToString(), userId.ToString(), userBookings.Count, _bookingSettings.MaxUserBookings);

            var newBooking = Booking.Create(eventId, userId);
            await bookingRepository.AddAsync(newBooking, ct);
            await eventRepository.UpdateAsync(_event, ct);
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
            throw new KeyNotExistException(bookingId, ConstantValues.key_not_found_exception);
        return booking;
    }
}