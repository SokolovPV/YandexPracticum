namespace EventFlow.Bookings.Application.Options;

/// <summary>
/// Класс c настройками бронирования
/// </summary>
public class BookingOptions
{
    /// <summary>
    /// максимальное количество бронирований пользователя 
    /// </summary>
    public int MaxUserBookings { get; set; }
}