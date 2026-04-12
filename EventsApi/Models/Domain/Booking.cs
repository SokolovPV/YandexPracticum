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
}
