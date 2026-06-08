using EventsApi.Domain.Entities;

namespace EventsApi.Application.Interfaces;
/// <summary>
/// Интерфейс сервиса бронирования
/// </summary>
public interface IBookingService
{
    /// <summary>
    ///  Создание брони для указанного события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">ID </param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct);
}
