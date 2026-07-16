namespace EventFlow.Entities.Brokers;

/// <summary>
/// Контракт события подтвержденного бронирования
/// </summary>
/// <param name="MessageId">идентификатор сообщения</param>
/// <param name="BookingId">идентификатор бронирования</param>
/// <param name="EventId">идентификатор события</param>
/// <param name="UserId">идентификатор пользователя</param>
/// <param name="ConfirmedAt">время подтверждения бронирования</param>
public record BookingConfirmed( Guid MessageId, Guid BookingId, Guid EventId, Guid UserId, DateTime ConfirmedAt);
