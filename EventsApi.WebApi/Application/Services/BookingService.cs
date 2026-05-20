using EventsApi.DataAccess;
using EventsApi.Models.Domain;
using EventsApi.WebApi.Application.CustomException;
using EventsApi.WebApi.Application.Interfaces;
namespace EventsApi.WebApi.Application.Services;
/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository, ILogger<BookingService> logger) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct)
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
                throw new KeyNotExistException( eventId, ConstantValues.key_not_found_exception);
            }
            if (!_event.TryReserveSeats())
                throw new NoAvailableSeatsException($"Для события ID={_event.Id} отстутствуют свободные места для бронирования");

            var newBooking = Booking.Create(eventId);
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