using EventsApi.Interfaces;
using EventsApi.ModelDTO;
using EventsApi.Models;
using System;

namespace EventsApi.Services
{
  public class EventService : IEventService
  {
    public static List<Event> Events { get; set; } = [];

    /// <summary>
    /// Метод получения мероприятия по идентификатору
    /// </summary>
    public EventDTO GetEvent(Guid id)
    {
      var _event =  Events.FirstOrDefault(q => q.Id == id);
      return _event is null ? null : new EventDTO(Id: _event.Id, Title: _event.Title, Description: _event.Description, StartAt: _event.StartAt, EndAt: _event.EndAt);
    }

    /// <summary>
    /// Метод получения мероприятий
    /// </summary>
    /// <returns></returns>
    public List<EventDTO> GetEvents()
    {
      return Events.Select(q => new EventDTO(Id: q.Id, Title: q.Title, Description: q.Description, StartAt: q.StartAt, EndAt: q.EndAt)).ToList();
    }

    /// <summary>
    /// Метод добавления мероприятия
    /// </summary>
    public void AddEvent(InputEventDTO createEventDTO)
    {
      Events.Add(new Event(
        title: createEventDTO.Title,
        description: createEventDTO.Description,
        startAt: createEventDTO.StartAt.Value,
        endAt: createEventDTO.EndAt.Value));
    }

    /// <summary>
    /// Метод изменения мероприятия
    /// </summary>
    public bool ChangeEvent(Guid id, InputEventDTO updateEvent)
    {
      var _event = Events.FirstOrDefault(q => q.Id == id);
      if (_event is null)
        return false;

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
      var _event =  Events.FirstOrDefault(q => q.Id == id);
      return _event is null ? false : Events.Remove(_event);
    }
  }
}
