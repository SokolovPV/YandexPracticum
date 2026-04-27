namespace EventsApi.Application.CustomException;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException() { }
    public NoAvailableSeatsException(string message) : base(message) { }
}