namespace EventFlow.Events.Domain.Exceptions;
/// <summary>
/// >Исключении, которое вызывается при отсутствии свободных мест
/// </summary>
public class NoAvailableSeatsException : Exception
{
    public Guid eventId { get; }
    public NoAvailableSeatsException() { }
    public NoAvailableSeatsException(string message) : base(message) { }
    public NoAvailableSeatsException(Guid eventId)
           : base($"Для события ID={eventId} отстутствуют свободные места для бронирования.")
    {
        this.eventId = eventId;
    }
}