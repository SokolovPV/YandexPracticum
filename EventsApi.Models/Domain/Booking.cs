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
    /// <summary>
    ///  внешний ключ на Event
    /// </summary>
    public Event? Event { get; private set; }

    private Booking() { }
    private Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
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
        if (Status == BookingStatus.Confirmed)
            return;
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отменяем бронирование
    /// </summary>
    public void Reject()
    {
        if (Status == BookingStatus.Rejected)
            return;
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
