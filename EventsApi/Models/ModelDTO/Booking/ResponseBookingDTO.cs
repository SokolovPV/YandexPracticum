namespace EventsApi.Models.ModelDTO.Booking;
/// <summary>
/// Модель DTO для отображения бронирования
/// </summary>
public record ResponseBookingDTO()
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
}
