namespace EventsApi.Models.ModelDTO.Booking;
/// <summary>
/// Модель DTO для отображения бронирования при запросе
/// </summary>
/// <param name="Id">Идентификатор бронирования</param>
/// <param name="EventID">Идентификатор события по которому создано бронирование</param>
/// <param name="Status">Статус бронирования</param>
/// <param name="CreatedAt">дата и время создания брони</param>
/// <param name="ProcessedAt">дата и время создания брони</param>
public record InfoBookingDTO(Guid Id, Guid EventID, string Status, DateTimeOffset CreatedAt, DateTimeOffset? ProcessedAt);
