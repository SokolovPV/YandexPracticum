using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json;
using EventFlow.Entities.Constant;
using EventFlow.Entities.Redis;
using EventFlow.Events.Application.DTO;
using EventFlow.Events.Application.Interfaces;
using EventFlow.Events.Application.Options;
using EventFlow.Events.Domain.Entities;
using EventFlow.Events.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace EventFlow.Events.Application.Services;
/// <summary>
/// Сервис для работы с событиями
/// </summary>
public class EventService(IEventRepository _repository,
                            ICacheService _cache,
                            IOptions<RedisOptions> redisOptions,
                            ILogger<EventService> _logger) : IEventService
{

    /// <inheritdoc/>
    public async Task<EventInfoDTO> CreateEventAsync(CreateEventDTO createEventDTO, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Создание события: {Title}", createEventDTO.Title);

        if (createEventDTO.StartAt.HasValue && createEventDTO.EndAt.HasValue &&
            createEventDTO.StartAt > createEventDTO.EndAt)
            throw new ValidationException(StringConstant.dateFrom_more_dateTo_exception);
        if (createEventDTO.TotalSeats > 100 || createEventDTO.TotalSeats < 1)
            throw new ValidationException(StringConstant.totalSeats_more_range_exception);

        var _event = Event.Create(
            title: createEventDTO.Title,
            description: createEventDTO.Description,
            startAt: createEventDTO.StartAt!.Value,
            endAt: createEventDTO.EndAt!.Value,
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

        var eventDto = await GetAndSetValueFromCacheAsync(
            RedisKeys.ForEvent(eventId),
            async () =>
            {
                var _event = await _repository.GetByIdAsync(eventId, ct);
                if (_event == null)
                    throw new KeyNotExistException(eventId.ToString(), nameof(Event));
                return new EventInfoDTO(
                    Id: _event.Id,
                    Title: _event.Title,
                    Description: _event.Description,
                    StartAt: _event.StartAt,
                    EndAt: _event.EndAt,
                    TotalSeats: _event.TotalSeats,
                    AvailableSeats: _event.AvailableSeats);
            },
           redisOptions.Value.SingleExpirationTTL);

        return eventDto;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult> GetEventsAsync(EventsFilter filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Получение событий с пагинацией. Страница: {Page}, Фильтр: {Filter}", filter.page, filter.title);

        //формируем  фильтр по рараметрам из запроса
        Expression<Func<Event, bool>> query = e =>
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
    public async Task ChangeEventAsync(Guid eventId, UpdateEventDTO? updateEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (updateEvent == null)
        {
            _logger.LogInformation("Обновление события {eventId} не требуется", eventId);
            return; // обновление не требуется, модель null 
        }
        _logger.LogInformation("Обновление события {eventId}", eventId);
        if (updateEvent.StartAt.HasValue && updateEvent.EndAt.HasValue &&
            updateEvent.StartAt > updateEvent.EndAt)
        {
            _logger.LogWarning("При обновлении события {eventId} дата начала мероприятия {StartAt} позже даты завершения {EndAt}.",
                eventId,
                updateEvent.StartAt,
                updateEvent.EndAt);
            throw new ValidationException(StringConstant.dateFrom_more_dateTo_exception);// false;
        }

        if (updateEvent.TotalSeats.HasValue && (updateEvent.TotalSeats > 100 || updateEvent.TotalSeats < 1))
        {
            _logger.LogWarning("При обновлении события {eventId} общее количество мест на должно быть больше 1 и меньше 100. TotalSeats: {TotalSeats}",
                eventId,
                updateEvent.TotalSeats);
            throw new ValidationException(StringConstant.totalSeats_more_range_exception);
        }

        var _event = await _repository.GetByIdAsync(eventId, ct);
        if (_event is null)
        {
            _logger.LogWarning("Ошибка обновления: событие не найдено. Идентификатор события: {eventId}", eventId);
            throw new KeyNotExistException(eventId.ToString(), nameof(Event));
        }
        if (updateEvent.TotalSeats.HasValue && (updateEvent.TotalSeats < (_event.TotalSeats - _event.AvailableSeats))) // с учетом уже занятых мест
        {
            _logger.LogWarning("При обновлении события {eventId} общее количество мест: {TotalSeats} меньше количества свободных мест.",
                eventId,
                updateEvent.TotalSeats);
            throw new ValidationException(StringConstant.totalSeats_less_availableSeats_exception);
        }


        _event.Title = updateEvent.Title == null ? _event.Title : updateEvent.Title;
        _event.Description = updateEvent.Description == null ? _event.Description : updateEvent.Description;
        _event.EndAt = updateEvent.EndAt.HasValue ? updateEvent.EndAt.Value : _event.EndAt;
        _event.StartAt = updateEvent.StartAt.HasValue ? updateEvent.StartAt.Value : _event.StartAt;
        if (updateEvent.TotalSeats.HasValue)
        {
            var newTotal = updateEvent.TotalSeats.Value;
            var oldTotal = _event.TotalSeats;
            var oldAvailable = _event.AvailableSeats;

            _event.AvailableSeats = Math.Min(oldAvailable + (newTotal - oldTotal), newTotal);
            _event.AvailableSeats = Math.Max(_event.AvailableSeats, 0);
        }
        _event.TotalSeats = updateEvent.TotalSeats.HasValue ? updateEvent.TotalSeats.Value : _event.TotalSeats;

        await _repository.UpdateAsync(_event, ct);
        // при обновлении события - удаляем событие из кэша 
        await _cache.KeyDeleteAsync(RedisKeys.ForEvent(eventId));
        _logger.LogInformation("Событие обновлено. ID: {eventId}", eventId);
    }

    /// <inheritdoc/>
    public async Task RemoveEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Удаление события ID: {eventId}", eventId);

        if (!await _repository.DeleteAsync(eventId, ct))
        {
            throw new KeyNotExistException(eventId.ToString(), nameof(Event));
        }
        await _cache.KeyDeleteAsync(RedisKeys.ForEvent(eventId));
        _logger.LogInformation("Событие удалено. ID: {eventId} ", eventId);

    }

    public async Task<bool> TryReserveSeatAsync(Guid eventId, CancellationToken ct)
    {
        var existedEvent = await _repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
            throw new KeyNotExistException(nameof(Event), eventId.ToString());

        if (DateTime.UtcNow > existedEvent.StartAt)
            throw new EventAlreadyStartedException(existedEvent.Id.ToString(), existedEvent.StartAt);


        var state = existedEvent.TryReserveSeats();
        if (!state)
            return false;

        await _repository.UpdateAsync(existedEvent, ct);

        // при изменения события удаляем из кеша событие и топ 10
        await _cache.KeyDeleteAsync(RedisKeys.ForEvent(eventId));
        await _cache.KeyDeleteAsync(RedisKeys.TopEvents);
        return true;
    }

    public async Task<bool> ReleaseSeatAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await _repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            throw new KeyNotExistException(nameof(Event), eventId.ToString());
        }

        existedEvent.ReleaseSeats();
        await _repository.UpdateAsync(existedEvent, ct);
        await _cache.KeyDeleteAsync(RedisKeys.ForEvent(eventId));
        await _cache.KeyDeleteAsync(RedisKeys.TopEvents);
        return true;
    }

    public async Task<PaginatedResultTop10> GetTop10EventsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Получение 10 популярных событий.");
        var result = await GetAndSetValueFromCacheAsync(
            RedisKeys.TopEvents,
            async () =>
            {
                var events = await _repository.GetTop10EventAsync(ct); // Получаем список топ-10 событий
                return new PaginatedResultTop10(
                    Events: events.Select(q => new EventInfoDTO(Id: q.Id,
                                                                Title: q.Title,
                                                                Description: q.Description,
                                                                StartAt: q.StartAt,
                                                                EndAt: q.EndAt,
                                                                TotalSeats: q.TotalSeats,
                                                                AvailableSeats: q.AvailableSeats)).ToList());
            },
            redisOptions.Value.TopExpirationTTL);

        return result;
    }

    /// <summary>
    /// Получение значения или добавление его в кэш
    /// </summary>
    private async Task<T?> GetAndSetValueFromCacheAsync<T>(
        string cacheKey,
        Func<Task<T>> getValueTask,
        int expirationtimeinminutes)
    {
        var cachedValue = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedValue))
            return JsonSerializer.Deserialize<T>(cachedValue);

        var result = await getValueTask();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(expirationtimeinminutes));
        return result;
    }

}