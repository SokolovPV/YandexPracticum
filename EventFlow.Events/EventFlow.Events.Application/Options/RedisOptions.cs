namespace EventFlow.Events.Application.Options;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Время жизни значения в кэше для одного ключа
    /// </summary>
    public int SingleExpirationTTL { get; set; }

    /// <summary>
    /// Время жидни значения в кэше для «топ-10 самых популярных событий»
    /// </summary>
    public int TopExpirationTTL { get; set; }
}