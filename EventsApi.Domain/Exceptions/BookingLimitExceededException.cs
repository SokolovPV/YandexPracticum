namespace EventsApi.Domain.Exceptions;

/// <summary>
/// Исключение, для случая "превышен лимит активных бронирований для события"
/// </summary>
public class BookingLimitExceededException : Exception
{
    // Дополнительное свойство для бизнес-данных
    public string EventId { get; } = null!;
    public int CurrentBookings { get; }
    public int MaxLimit { get; }

    public BookingLimitExceededException()
            : base("Превышен лимит активных бронирований для события.") { }

    public BookingLimitExceededException(string message) : base(message) { }
    public BookingLimitExceededException(string eventId, int currentCount, int maxLimit)
    : base($"Превышен лимит активных бронирований для события {eventId}. " +
           $"Текущее количество: {currentCount}, максимум: {maxLimit}")
    {
        EventId = eventId;
        CurrentBookings = currentCount;
        MaxLimit = maxLimit;
    }
}