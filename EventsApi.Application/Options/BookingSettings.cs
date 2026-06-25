namespace EventsApi.Application.Options;

/// <summary>
/// Класс c настройками бронирования
/// </summary>
public class BookingSettings
{
    /// <summary>
    /// максимальное количество бронирований пользователя 
    /// </summary>
    public int MaxUserBookings { get; set; }
}