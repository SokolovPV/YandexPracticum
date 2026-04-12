using EventsApi.Models.ModelDTO.Event;

namespace EventsApi.Application.Interfaces;
/// <summary>
/// Интерфейс сервиса событий
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Создание события
    /// </summary>
    /// <param name="createEventDTO">Входящая модель создания события</param>
    /// <param name="ct">токен отмены</param>
    /// <returns></returns>
    Task<ResponseEventDTO> AddEventAsync(InputEventDTO createEventDTO, CancellationToken ct);


    /// <summary>
    /// Получение событий с пагинацией
    /// </summary>
    /// <param name="filter">Фильтр для пагинации</param>
    /// <param name="ct">Токен отмены</param>
    Task<PaginatedResult> GetEventsAsync(EventsFilter filter, CancellationToken ct);


    /// <summary>
    /// Поиск события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<ResponseEventDTO?> GetEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="updateEvent">Входящая модель обновления события </param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task ChangeEventAsync(Guid eventId, InputEventDTO updateEvent, CancellationToken ct);


    /// <summary>
    /// Удаление события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task RemoveEventAsync(Guid eventId, CancellationToken ct);
}
