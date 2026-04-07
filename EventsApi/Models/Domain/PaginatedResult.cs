using EventsApi.Models.ModelDTO;

namespace EventsApi.Models.Domain
{
  /// <summary>
  /// Модель ответа со списком мероприятий 
  /// </summary>
  /// <param name="Events">список мероприятий</param>
  /// <param name="Page"> номер текущей страницы</param>
  /// <param name="PageSize">количество мероприятий на текущей странице</param>
  /// <param name="TotalItems">общее количество мероприятий</param>
  public record PaginatedResult(
    List<EventDTO> Events,
    int Page,
    int PageSize,
    int TotalItems);
}
