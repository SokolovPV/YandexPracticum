namespace EventsApi.WebApi.Application.CustomException;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException() { }
    public NoAvailableSeatsException(string message) : base(message) { }
}