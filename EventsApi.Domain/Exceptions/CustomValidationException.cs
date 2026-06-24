namespace EventsApi.Domain.Exceptions;

/// <summary>
/// Исключение для ошибок валидации 
/// </summary>
public class CustomValidationException : Exception
{
    public string EntityName { get; } = null!;
    public string EntityId { get; }  = null!;
    public CustomValidationException() : base() { }
    public CustomValidationException(string message) : base(message) { }

   public CustomValidationException(string message, string entity, string id ) : base(message)
    {
        EntityName = entity;
        EntityId = id;
    }
}