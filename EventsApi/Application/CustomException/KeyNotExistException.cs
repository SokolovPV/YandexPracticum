namespace EventsApi.Application.CustomException;

public class KeyNotExistException : Exception
{
  public Guid Id { get; }
  public KeyNotExistException() { }

  public KeyNotExistException(Guid Id, string message) : base(message)
  {
    this.Id = Id;
  }

  public KeyNotExistException(Guid Id, string message, Exception innerException) : base(message, innerException)
  {
    this.Id = Id;
  }
}