using EventsApi.Application.CustomException;
using EventsApi.Application.Interfaces;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;
namespace EventsApi.Application.Services;
/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(IEventService eventService, IBookingRepository repository, ILogger<BookingService> logger) : IBookingService
{
    private const string key_not_found_exception = "Идентификатор бронирования не найден.";
    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Создание новой брони для события: {Event}", eventId);

        //если событие не найдено - вернет Status404NotFound
        await eventService.GetEventAsync(eventId, ct);

        var newBooking = new Booking(eventId);
        await repository.AddAsync(newBooking, ct);
        logger.LogInformation("Бронирование создано. ID: {Id} ", newBooking.Id);
        return newBooking;
    }
    /// <inheritdoc/>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Получение бронирования : {bookingId}", bookingId);
        var booking = await repository.GetByIdAsync(bookingId, ct);
        if (booking is null)
            throw new KeyNotExistException(bookingId, key_not_found_exception);
        return booking;
    }
}