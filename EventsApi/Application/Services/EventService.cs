using System.ComponentModel.DataAnnotations;
using EventsApi.Application.CustomException;
using EventsApi.Application.Interfaces;
using EventsApi.Infrastructure.Interfaces;
using EventsApi.Models.Domain;
using EventsApi.Models.ModelDTO.Event;
namespace EventsApi.Application.Services;
/// <summary>
/// Сервис для работы с событиями
/// </summary>
public class EventService(IEventRepository _repository, ILogger<EventService> _logger) : IEventService
{
    private const string key_not_found_exception = "Идентификатор мероприятия не найден.";
    private const string dateFrom_more_dateTo_exception = "Дата начала мероприятия больше даты завершения.";
    private const string totalSeats_more_range_exception = "Общее количество мест на событие должно быть больше 1 и меньше 100.";

    /// <inheritdoc/>
    public async Task<EventInfoDTO> CreateEventAsync(CreateEventDTO createEventDTO, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Создание события: {Title}", createEventDTO.Title);

        if (createEventDTO.StartAt.HasValue && createEventDTO.EndAt.HasValue &&
            createEventDTO.StartAt > createEventDTO.EndAt)
            throw new ValidationException(dateFrom_more_dateTo_exception);
        if (createEventDTO.TotalSeats > 100 || createEventDTO.TotalSeats < 1)
            throw new ValidationException(totalSeats_more_range_exception);

        var _event = Event.Create(
            title: createEventDTO.Title,
            description: createEventDTO.Description,
            startAt: createEventDTO.StartAt.Value,
            endAt: createEventDTO.EndAt.Value,
            totalSeats: createEventDTO.TotalSeats);
        await _repository.AddAsync(_event, ct);
        _logger.LogInformation("Событие создано. Идентификатор события: {EventId}", _event.Id);
        return new EventInfoDTO(
            Id: _event.Id,
            Title: _event.Title,
            Description: _event.Description,
            StartAt: _event.StartAt,
            EndAt: _event.EndAt,
            TotalSeats: _event.TotalSeats,
            AvailableSeats: _event.AvailableSeats);
    }

    /// <inheritdoc/>
    public async Task<EventInfoDTO?> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Получение события: {eventId}", eventId);
        var _event = await _repository.GetByIdAsync(eventId, ct);
        if (_event == null)
            throw new KeyNotExistException(eventId, key_not_found_exception);

        return new EventInfoDTO(
                Id: _event.Id,
                Title: _event.Title,
                Description: _event.Description,
                StartAt: _event.StartAt,
                EndAt: _event.EndAt,
                TotalSeats: _event.TotalSeats,
                AvailableSeats: _event.AvailableSeats);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult> GetEventsAsync(EventsFilter filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Получение событий с пагинацией. Страница: {Page}, Фильтр: {Filter}", filter.page, filter.title);

        //формируем  фильтр по рараметрам из запроса
        Func<Event, bool> query = e =>
        (string.IsNullOrEmpty(filter.title) || e.Title.Contains(filter.title, StringComparison.OrdinalIgnoreCase)) &&
        (!filter.from.HasValue || e.StartAt >= filter.from) &&
        (!filter.to.HasValue || e.EndAt <= filter.to);


        int filteredCount = await _repository.CountAsync(query, ct);
        var events = await _repository.ListAsync(query, filter.page, filter.pageSize, ct);

        return new PaginatedResult(
          Events: events.Select(q => new EventInfoDTO(Id: q.Id,
                                                        Title: q.Title,
                                                        Description: q.Description,
                                                        StartAt: q.StartAt,
                                                        EndAt: q.EndAt,
                                                        TotalSeats: q.TotalSeats,
                                                        AvailableSeats: q.AvailableSeats)).ToList(),
          Page: filter.page,
          PageSize: filter.pageSize,
          TotalItems: filteredCount);
    }

    /// <inheritdoc/>
    public async Task ChangeEventAsync(Guid eventId, UpdateEventDTO updateEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Обновление события {eventId}", eventId);
        if (updateEvent.StartAt.HasValue && updateEvent.EndAt.HasValue &&
            updateEvent.StartAt > updateEvent.EndAt)
        {
            _logger.LogError(dateFrom_more_dateTo_exception);
            throw new ValidationException(dateFrom_more_dateTo_exception);// false;
        }

        if (updateEvent.TotalSeats > 100 || updateEvent.TotalSeats < 1)
        {
            _logger.LogError(totalSeats_more_range_exception);
            throw new ValidationException(totalSeats_more_range_exception);
        }

        var _event = await _repository.GetByIdAsync(eventId, ct);
        if (_event is null)
        {
            _logger.LogError("Ошибка обновления: событие не найдено. Идентификатор ID: {eventId}", eventId);
            throw new KeyNotExistException(eventId, key_not_found_exception);
        }

        _event.Title = updateEvent.Title;
        _event.Description = updateEvent.Description;
        _event.EndAt = updateEvent.EndAt.Value;
        _event.StartAt = updateEvent.StartAt.Value;

        await _repository.UpdateAsync(_event, ct);
        _logger.LogInformation("Событие обновлено. ID: {eventId}", eventId);
    }

    /// <inheritdoc/>
    public async Task RemoveEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Удаление события ID: {eventId}", eventId);

        if (!await _repository.DeleteAsync(eventId, ct))
        {
            throw new KeyNotExistException(eventId, key_not_found_exception);
        }
        _logger.LogInformation("Событие удалено. ID: {eventId} ", eventId);
    }
}