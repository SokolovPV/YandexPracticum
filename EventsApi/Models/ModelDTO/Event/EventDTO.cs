namespace EventsApi.Models.ModelDTO.Event
{
  /// <summary>
  /// Модель для отображения мероприятия
  /// </summary>
  /// <param name="Id">Идентификатор мероприятия</param>
  /// <param name="Title">Название мероприятия</param>
  /// <param name="Description">Описание</param>
  /// <param name="StartAt">Дата начала</param>
  /// <param name="EndAt">Дата окончания</param>
  public record EventDTO(Guid Id, string Title, string? Description, DateTime StartAt, DateTime EndAt);
}
