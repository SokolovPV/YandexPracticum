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
    /// <param name="userId">идентификатор пользователя</param>
    /// <param name="ct">Токен отмены</param>
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">ID </param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Метод отменяет бронирование
    /// </summary>
    /// <param name="bookingId">идентификатор бронирования</param>
    /// <param name="userId">идентификатор пользователя</param>
    /// <param name="ct">токен отмены</param>
    /// <returns></returns>
    Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken ct);
}
