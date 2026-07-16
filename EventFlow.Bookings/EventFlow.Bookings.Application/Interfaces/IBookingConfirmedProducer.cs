
namespace EventFlow.Bookings.Application.Interfaces;

/// <summary>
/// Контракт публикации события подтвержденной брони.
/// </summary>
public interface IBookingConfirmedProducer
{
    /// <summary>
    /// Публикует сериализованный payload в брокер
    /// </summary>
    /// <param name="topic">Топики</param>
    /// <param name="key">Ключ сообщения</param>
    /// <param name="payload">payload</param>
    /// <param name="ct">Токен отмены</param>
    Task PublishAsync(string topic, string key, string payload, CancellationToken ct);
}