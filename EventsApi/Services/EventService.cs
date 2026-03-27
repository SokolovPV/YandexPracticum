using EventsApi.CustomException;
using EventsApi.Interfaces;
using EventsApi.ModelDTO;
using EventsApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EventsApi.Services
{
  public class EventService : IEventService
  {
    private const int page_default = 1;
    private const int pageSize_default = 10;
    private const string key_not_found_exception = "Идентификатор мероприятия не найден.";
    private const string dateFrom_more_dateTo_exception = "Дата начала мероприятия больше даты завершения.";
    public static List<Event> Events { get; set; } = [];

    /// <summary>
    /// Метод получения мероприятия по идентификатору
    /// </summary>
    public EventDTO? GetEvent(Guid id)
    {
      var _event = Events.FirstOrDefault(q => q.Id == id);
      if (_event == null)
        throw new KeyNotExistException(id, key_not_found_exception);

      return new EventDTO(Id: _event.Id, Title: _event.Title, Description: _event.Description, StartAt: _event.StartAt, EndAt: _event.EndAt);
    }

    /// <summary> Метод получения мероприятий </summary>
    /// <param name="title"> поиск по названию </param>
    /// <param name="from">события, которые начинаются не раньше указанной даты</param>
    /// <param name="to">события, которые заканчиваются не позже указанной даты</param>
    /// <param name="page">страница, которую необходимо вернуть</param>
    /// <param name="pageSize">количество элементов на странице</param>
    public PaginatedResult GetEvents(string? title, DateTime? from, DateTime? to, int? page, int? pageSize)
    {
      var _event = Events.AsEnumerable();
      var _page = page ?? page_default;
      var _pageSize = pageSize ?? pageSize_default;

      if(_page == 0 || _page > 1000 || _pageSize == 0 || _pageSize > 1000)
        throw new ValidationException("Некорректные значения для пагинации.");

        // на случай если отрицательные числа номер и размер страницы
      _page = Math.Abs(_page);
      _pageSize = Math.Abs(_pageSize);

      if (!string.IsNullOrEmpty(title))
        _event = _event.Where(q => q.Title.Contains(title, comparisonType: StringComparison.CurrentCultureIgnoreCase));

      if (from.HasValue)
        _event = _event.Where(q => q.StartAt >= from);

      if (to.HasValue)
        _event = _event.Where(q => q.EndAt <= to);

      int filteredCount = _event.Count();
      var items = _event
         .Skip((_page - 1) * _pageSize)
         .Take(_pageSize)
         .Select(q => new EventDTO(Id: q.Id, Title: q.Title, Description: q.Description, StartAt: q.StartAt, EndAt: q.EndAt))
         .ToList();

      return new PaginatedResult(
        Events: items,
        Page: _page,
        PageSize: _pageSize,
        TotalItems: filteredCount);
    }

    /// <summary>
    /// Метод добавления мероприятия
    /// </summary>
    public EventDTO AddEvent(InputEventDTO createEventDTO)
    {
      if (createEventDTO.StartAt > createEventDTO.EndAt)
        throw new ValidationException(dateFrom_more_dateTo_exception);// false;

      var _event = new Event(title: createEventDTO.Title, description: createEventDTO.Description, startAt: createEventDTO.StartAt.Value, endAt: createEventDTO.EndAt.Value);
      Events.Add(_event);
      return new EventDTO(Id: _event.Id, Title: _event.Title, Description: _event.Description, StartAt: _event.StartAt, EndAt: _event.EndAt);
    }

    /// <summary>
    /// Метод изменения мероприятия
    /// </summary>
    public bool ChangeEvent(Guid id, InputEventDTO updateEvent)
    {
      if (updateEvent.StartAt > updateEvent.EndAt)
        throw new ValidationException(dateFrom_more_dateTo_exception);// false;

      var _event = Events.FirstOrDefault(q => q.Id == id);
      if (_event is null)
        throw new KeyNotExistException(id, key_not_found_exception);

      _event.Title = updateEvent.Title;
      _event.Description = updateEvent.Description;
      _event.EndAt = updateEvent.EndAt.Value;
      _event.StartAt = updateEvent.StartAt.Value;

      return true;
    }

    /// <summary>
    /// Метод удаления мероприятия
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool RemoveEvent(Guid id)
    {
      var _event = Events.FirstOrDefault(q => q.Id == id);
      if (_event is null)
        throw new KeyNotExistException(id, key_not_found_exception);

      return Events.Remove(_event);
    }
  }
}
