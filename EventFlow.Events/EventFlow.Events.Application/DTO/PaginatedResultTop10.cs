namespace EventFlow.Events.Application.DTO;

/// <remarks>
/// Список событий ТОП 10
/// </remarks>
/// <param name="Events">Список событий</param>
public record PaginatedResultTop10(List<EventInfoDTO> Events);