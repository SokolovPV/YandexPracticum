using System.ComponentModel.DataAnnotations;

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
  public DateTimeOffset StartAt { get; set; }

  /// <summary>Дата окончания мероприятия</summary>
  public DateTimeOffset EndAt { get; set; }

  /// <summary>общее количество мест на событии</summary>
  public int TotalSeats { get; set; }

  /// <summary>текущее количество свободных мест</summary>
  public int AvailableSeats { get; set; }

  /// <summary>список бронирований</summary>
  internal ICollection<Booking> Bookings { get; private set; } = [];



  private Event(
      string title,
      DateTimeOffset startAt,
      DateTimeOffset endAt,
      int totalSeats = 1,
      string? description = default) : this()
  {
    Id = Guid.NewGuid();
    Title = title;
    Description = description;
    StartAt = startAt;
    EndAt = endAt;
    TotalSeats = totalSeats;
    AvailableSeats = totalSeats;
  }

  /// <summary>
  /// Метод создания события
  /// </summary>
  /// <param name="title">Заголовок события</param>
  /// <param name="startAt">Дата начала события</param>
  /// <param name="endAt">Дата окончания события</param>
  /// <param name="totalSeats">Общее количество мест на событии</param>
  /// <param name="description">Описание события</param>
  public static Event Create(string title, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats, string? description = null)
  {
    if (startAt > endAt)
      throw new ValidationException("Дата начала события не может быть позже даты окончания");

    if (totalSeats < 1 || totalSeats > 100)
      throw new ValidationException("Общее количество мест на событие должно быть больше 1 и меньше 100");

    return new Event(title, startAt, endAt, totalSeats, description);
  }

  /// <summary>
  /// Метод для занятия мест на событие
  /// </summary>
  /// <param name="count">Количество занимаемых мест</param>
  /// <returns></returns>
  public bool TryReserveSeats(int count = 1)
  {
    if (count > AvailableSeats || count < 1)
      return false;

    AvailableSeats -= count;
    return true;
  }

  /// <summary>
  /// Метод для освобождения мест (при отклонении брони) 
  /// </summary>
  /// <param name="count">Количество освобождаемых мест на событие</param>
  /// <returns></returns>
  public void ReleaseSeats(int count = 1)
  {
    if (count + AvailableSeats >= TotalSeats)
      AvailableSeats = TotalSeats;
    else
      AvailableSeats += count;
  }
}
