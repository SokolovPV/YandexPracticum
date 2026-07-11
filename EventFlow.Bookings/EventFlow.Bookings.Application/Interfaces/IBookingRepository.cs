using System.Linq.Expressions;
using EventFlow.Bookings.Domain.Entities;

namespace EventFlow.Bookings.Application.Interfaces;

public interface IBookingRepository
{
    /// <summary>
    /// Добавление бронирования в репозиторий
    /// </summary>
    /// <param name="booking">Бронирование</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(Booking booking, CancellationToken ct);

    /// <summary>
    /// Удаление бронирования из репозитория
    /// </summary>
    /// <param name="bookingId">ID брони</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> DeleteAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Получение бронирования по ID
    /// </summary>
    /// <param name="bookingId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Обновление бронирования
    /// </summary>
    /// <param name="booking">Бронирование</param>
    /// <param name="ct">Токен отмены</param>
    Task UpdateAsync(Booking booking, CancellationToken ct);


    /// <summary>
    /// Получение бронирований с фильтрацией
    /// </summary>
    /// <param name="query">Предикат для фильтрации событий</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<Booking>> ListAsync(Expression<Func<Booking, bool>> query, CancellationToken ct);

    /// <summary>
    /// Подтверждение бронирования
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> ConfirmAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Отмена бронирования
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> RejectAsync(Guid bookingId, CancellationToken ct);
}
