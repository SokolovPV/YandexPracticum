using EventsApi.Models.ModelDTO.Booking;

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
    Task<ResponseBookingDTO> CreateBookingAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">ID </param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task<ResponseBookingDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct);
}
