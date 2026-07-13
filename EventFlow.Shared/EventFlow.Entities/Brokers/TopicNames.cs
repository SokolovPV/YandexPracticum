namespace EventFlow.Entities.Brokers;

/// <summary>
/// Имя Kafka-топика, общее для издателя и подписчика
/// </summary>
public static class TopicNames
{
    /// <summary>
    /// Топик подтвержденных бронирований
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}