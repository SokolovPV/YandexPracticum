namespace EventFlow.Booking.Application.DTO;
/// <summary>
/// Модель DTO для отображения бронирования при запросе
/// </summary>
/// <param name="Id">Идентификатор бронирования</param>
/// <param name="EventID">Идентификатор события по которому создано бронирование</param>
/// <param name="UserID">Идентификатор пользователя забронировавшего событие</param>
/// <param name="Status">Статус бронирования</param>
/// <param name="CreatedAt">дата и время создания брони</param>
/// <param name="ProcessedAt">дата и время создания брони</param>
public record InfoBookingDTO(Guid Id, Guid EventID, Guid UserID, string Status, DateTime CreatedAt, DateTime? ProcessedAt);
