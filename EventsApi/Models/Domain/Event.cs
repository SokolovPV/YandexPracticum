namespace EventsApi.Models.Domain;
/// <summary>
/// Модель мероприятия
/// </summary>
public class Event()
{
  /// <summary>Идентификатор мероприятия</summary>
  public Guid Id { get; set; }

  /// <summary>Название мероприятия</summary>   
  public string Title { get; set; }

  /// <summary>Описание мероприятия</summary>
  public string? Description { get; set; }

  /// <summary>Дата начала мероприятия</summary>
  public DateTime StartAt { get; set; }

  /// <summary>Дата окончания мероприятия</summary>
  public DateTime EndAt { get; set; }

  public Event(string title, string description, DateTime startAt, DateTime endAt) : this()
  {
    Id = Guid.NewGuid();
    Title = title;
    Description = description;
    StartAt = startAt;
    EndAt = endAt;
  }
}
