namespace EventFlow.Bookings.Domain.Exceptions;

/// <summary>
/// Исключение, для случая "превышен лимит активных бронирований для события"
/// </summary>
public class BookingLimitExceededException : Exception
{
    // Дополнительное свойство для бизнес-данных
    public string Login { get; } = null!;
    public int CurrentBookings { get; }
    public int MaxLimit { get; }

    public BookingLimitExceededException()
            : base("Превышен лимит активных бронирований для события.") { }

    public BookingLimitExceededException(string message) : base(message) { }
    public BookingLimitExceededException(string login, int currentCount, int maxLimit)
    : base($"Превышен лимит активных бронирований пользователя {login}. " +
           $"Текущее количество: {currentCount}, максимум: {maxLimit}")
    {
        Login = login;
        CurrentBookings = currentCount;
        MaxLimit = maxLimit;
    }
}