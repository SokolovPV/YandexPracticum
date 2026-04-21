using System.Runtime.InteropServices;

namespace EventsApi.Models.Domain;
/// <summary>
/// Модель бронирования
/// </summary>
public class Booking
{
    /// <summary>
    /// уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; set; }
    /// <summary>
    ///  текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; }
    /// <summary>
    /// дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    ///  дата и время обработки брони.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.Now;
        EventId = eventId;
    }

    /// <summary>
    /// Метод создания события
    /// </summary>
    /// <param name="eventId">идентификатор события</param>
    public static Booking Create(Guid eventId)
    {
        return new Booking(eventId);
    }

    /// <summary>
    /// Подтверждаем бронирование
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.Now;
    }

    /// <summary>
    /// Отменяем бронирование
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.Now;
    }
}
