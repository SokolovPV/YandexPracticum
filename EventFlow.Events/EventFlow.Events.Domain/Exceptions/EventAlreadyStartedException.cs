namespace EventFlow.Events.Domain.Exceptions;

/// <summary>
/// Исключение, при бронирование если "Событие уже началось"
/// </summary>
public class EventAlreadyStartedException : Exception
{
    // Дополнительное свойство для бизнес-данных
    public string EventId { get; } = null!;
    public DateTime EventStartTime { get; } 

    public EventAlreadyStartedException()
        : base("Событие уже началось и недоступно для бронирования.") { }

    public EventAlreadyStartedException(string message) : base(message) { }

    public EventAlreadyStartedException(string eventId, DateTime startTime)
            : base($"Событие ID {eventId} уже началось в {startTime:HH:mm} и недоступно для бронирования.")
    {
        EventId = eventId;
        EventStartTime = startTime;
    }

}