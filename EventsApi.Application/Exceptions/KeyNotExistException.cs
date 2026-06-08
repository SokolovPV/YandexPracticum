using System;
namespace EventsApi.Application.Exceptions;

public class KeyNotExistException :  Exception
{
  public Guid eventId { get; }


  public KeyNotExistException(Guid eventId, string message) : base($"Элемент c ID: '{eventId}' не найден.")
  {
    this.eventId = eventId;
  }

  public KeyNotExistException(Guid eventId, string message, Exception innerException) : base(message, innerException)
  {
    this.eventId = eventId;
  }
}