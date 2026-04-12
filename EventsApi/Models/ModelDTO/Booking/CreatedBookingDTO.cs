namespace EventsApi.Models.ModelDTO.Booking;
/// <summary>
/// Модель DTO для отображения бронирования при создании
/// </summary>
public record CreatedBookingDTO()
{
    /// <summary>
    /// Идентификатор бронирования
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор события по которому создано бронирование
    /// </summary>
    public Guid EventID { get; init; }

    /// <summary>
    /// Статус бронирования
    /// </summary>
    public string Status { get; init; }

    /// <summary>
    /// дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
