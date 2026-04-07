using EventsApi.Models.Domain;
using System.Linq.Expressions;

namespace EventsApi.Infrastructure.Interfaces
{
    public interface IBookingRepository
    {
        /// <summary>
        /// Добавление бронирования в репозиторий
        /// </summary>
        /// <param name="booking">Само бронирование</param>
        /// <param name="ct">Токен отмены</param>
        Task AddAsync(Booking booking, CancellationToken ct);

        /// <summary>
        /// Удаление бронирования из репозитория
        /// </summary>
        /// <param name="id">ID брони</param>
        /// <param name="ct">Токен отмены</param>
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Получение бронирования по ID
        /// </summary>
        /// <param name="id">ID события</param>
        /// <param name="ct">Токен отмены</param>
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Обновление бронирования
        /// </summary>
        /// <param name="booking">Само событие</param>
        /// <param name="ct">Токен отмены</param>
        Task UpdateAsync(Booking booking, CancellationToken ct);


        /// <summary>
        /// Получение бронирований с фильтрацией
        /// </summary>
        /// <param name="query">Предикат для фильтрации событий</param>
        /// <param name="ct">Токен отмены</param>
        Task<List<Booking>> ListAsync(Func<Booking, bool> query, CancellationToken ct);
    }
}
