namespace EventFlow.Events.Application.Interfaces;

/// <summary>
/// Репозиторий хранения факта обработки сообщений
/// </summary>
public interface IProcessedMessageRepository
{
    /// <summary>
    /// Проверяем, что сообщение с указанным идентификатором обработано
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Добавляем запись об обработанном сообщении
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(Guid id, CancellationToken ct);
}