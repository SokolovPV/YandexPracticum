namespace EventsApi.Models.ModelDTO.Event;
/// <summary>
/// Модель для отображения мероприятия
/// </summary>
/// <param name="Id">Идентификатор мероприятия</param>
/// <param name="Title">Название мероприятия</param>
/// <param name="Description">Описание</param>
/// <param name="StartAt">Дата начала</param>
/// <param name="EndAt">Дата окончания</param>
/// <param name="TotalSeats">Общее количество мест на событии</param>
/// <param name="AvailableSeats">Текущее количество свободных мест</param>
public record EventInfoDTO(Guid Id, string Title, string? Description, DateTimeOffset StartAt, DateTimeOffset EndAt, int TotalSeats, int AvailableSeats);
