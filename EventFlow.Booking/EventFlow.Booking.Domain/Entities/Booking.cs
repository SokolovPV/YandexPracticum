using EventFlow.Booking.Domain.Enums;

namespace EventFlow.Booking.Domain.Entities;
/// <summary>
/// Модель бронирования
/// </summary>
public class Booking
{
    /// <summary>
    /// уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; private set; }
    /// <summary>
    /// Идентификатор пользователя, который создал бронь
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    ///  текущий статус брони
    /// </summary>
    public BookingStatus Status { get; private set; }
    /// <summary>
    /// дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>
    ///  дата и время обработки брони.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    private Booking() { }
    private Booking(Guid eventId, Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        EventId = eventId;
    }

    /// <summary>
    /// Фабричный метод создания бронирования
    /// </summary>
    /// <param name="eventId">идентификатор события</param>
    /// <param name="userId">идентификатор пользователя</param>
    public static Booking Create(Guid eventId, Guid userId)
    {
        return new Booking(eventId, userId);
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

    /// <summary>
    /// Отменяем бронирование
    /// </summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            return;
        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
