using EventsApi.Application.CustomException;
using EventsApi.Application.Interfaces;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Booking;
namespace EventsApi.Application.Services;
/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(IEventService eventService, IBookingRepository repository, ILogger<IBookingRepository> logger) : IBookingService
{
    private const string key_not_found_exception = "Идентификатор бронирования не найден.";
    /// <inheritdoc/>
    public async Task<ResponseBookingDTO> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Создание новой брони для события: {Event}", eventId);
        //если событие не найдено - вернет Status404NotFound
        await eventService.GetEventAsync(eventId, ct);

        var newBooking = new Booking(eventId);
        await repository.AddAsync(newBooking, ct);
        logger.LogInformation("Бронирование создано. ID: {Id} ", newBooking.Id);
        return new ResponseBookingDTO
        {
            Id = newBooking.Id,
            EventID = newBooking.EventId,
            Status = newBooking.Status.ToString()
        };
    }
    /// <inheritdoc/>
    public async Task<ResponseBookingDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Получение бронирования : {bookingId}", bookingId);
        var booking = await repository.GetByIdAsync(bookingId, ct);
        if (booking is null)
            throw new KeyNotExistException(bookingId, key_not_found_exception);
        return new ResponseBookingDTO
        {
            Id = booking.Id,
            EventID = booking.EventId,
            Status = booking.Status.ToString()
        };
    }
}