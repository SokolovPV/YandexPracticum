using System;
namespace EventsApi.Domain.Exceptions;

public class KeyNotExistException :  Exception
{
  public Guid eventId { get; }
   public KeyNotExistException(string message) : base(message) { }

  public KeyNotExistException(Guid eventId, string message) : base($"Элемент c ID: '{eventId}' не найден.")
  {
    this.eventId = eventId;
  }

  //"Идентификатор мероприятия {Id} не найден.", eventId

  public KeyNotExistException(Guid eventId, string message, Exception innerException) : base(message, innerException)
  {
    this.eventId = eventId;
  }
}