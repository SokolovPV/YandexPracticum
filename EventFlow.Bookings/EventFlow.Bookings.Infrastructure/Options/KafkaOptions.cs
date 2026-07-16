namespace EventFlow.Bookings.Infrastructure.Options;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Адрес Kafka сервера
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;
}
