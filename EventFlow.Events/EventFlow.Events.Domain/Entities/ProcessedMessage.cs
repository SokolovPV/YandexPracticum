namespace EventFlow.Events.Domain.Entities;

/// <summary>
/// Запись о сообщении, которое уже было обработано consumer-ом.
/// </summary>
public class ProcessedMessage
{
    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Время обработки.
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}