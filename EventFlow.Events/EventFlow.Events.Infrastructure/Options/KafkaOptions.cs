namespace EventFlow.Events.Infrastructure.Options;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Адрес Kafka сервера
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Имя consumer group
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;
}