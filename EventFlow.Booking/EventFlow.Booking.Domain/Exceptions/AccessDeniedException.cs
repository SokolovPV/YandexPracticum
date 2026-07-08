namespace EventFlow.Booking.Domain.Exceptions;

/// <summary>
/// Исключение, для случая "пользователь не имеет прав на выполнение действия"
/// </summary>
public sealed class AccessDeniedException : Exception
{
    // Дополнительные свойства
    public string? UserId { get; }
    public string? RequiredRole { get; }
    public string? ActionName { get; }


    // Базовые конструкторы
    public AccessDeniedException() 
        : base("Доступ запрещен. У пользователя недостаточно прав.") { }

    public AccessDeniedException(string message) : base(message) { }


    public AccessDeniedException(string userId, string action)
        : base($"Пользователь '{userId}' не имеет прав на выполнение действия '{action}'")
    {
        UserId = userId;
        ActionName = action;
    }
}