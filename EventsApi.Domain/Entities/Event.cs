
using System.ComponentModel.DataAnnotations;

namespace EventsApi.Domain.Entities;
/// <summary>
/// Модель мероприятия
/// </summary>
public class Event
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

  /// <summary>общее количество мест на событии</summary>
  public int TotalSeats { get; set; }

  /// <summary>текущее количество свободных мест</summary>
  public int AvailableSeats { get; set; }

  /// <summary>список бронирований</summary>
  public ICollection<Booking> Bookings { get; private set; } = [];


  private Event() { Title = null!; }
  private Event(
      string title,
      DateTime startAt,
      DateTime endAt,
      int totalSeats = 1,
      string? description = default)
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
  public static Event Create(
    string? title,
    DateTime startAt,
    DateTime endAt,
    int totalSeats,
    string? description = default)
  {
    if (string.IsNullOrWhiteSpace(title))
      throw new ValidationException("Название мероприятия не может быть пустым");

    if (title.Length > 200)
      throw new ValidationException("Название мероприятия не может превышать 200 символов");

    if (startAt > endAt)
      throw new ValidationException("Дата начала события не может быть позже даты окончания");

    if (startAt < DateTime.UtcNow)
      throw new ValidationException("Нельзя создать событие в прошлом");

    if (totalSeats < 1)
      throw new ValidationException("Количество мест должно быть больше 0");

    if (totalSeats > 1000)
      throw new ValidationException("Количество мест не может превышать 1000");

    return new Event(title!.Trim(), startAt, endAt, totalSeats, description);
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
