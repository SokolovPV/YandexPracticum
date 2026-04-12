using EventsApi.Models.Domain;

namespace EventsApi.Infrastructure.Interfaces;

public interface IEventRepository
{
    /// <summary>
    /// Добавление события в репозиторий
    /// </summary>
    /// <param name="_event">Само событие</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(Event _event, CancellationToken ct);

    /// <summary>
    /// Удаление события из репозитория
    /// </summary>
    /// <param name="id">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получение события по ID
    /// </summary>
    /// <param name="id">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="_event">Само событие</param>
    /// <param name="ct">Токен отмены</param>
    Task UpdateAsync(Event _event, CancellationToken ct);


    /// <summary>
    /// Получение событий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="query">Предикат для фильтрации событий</param>
    /// <param name="page">Номер страницы с данными для возврата</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<Event>> ListAsync(Func<Event, bool> query, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Получение количества элементов в базе по фильтру
    /// </summary>
    /// <param name="query">Предикат для фильтрации событий</param>
    /// <param name="ct">Токен отмены</param>
    Task<int> CountAsync(Func<Event, bool> query, CancellationToken ct);
}
