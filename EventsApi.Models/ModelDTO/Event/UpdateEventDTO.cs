using EventsApi.Models.Attribute;
using System.ComponentModel.DataAnnotations;
namespace EventsApi.Models.ModelDTO.Event;
/// <summary>Модель данных для создания мероприятия </summary>
public record UpdateEventDTO
{
    /// <summary>Название мероприятия</summary>
    public string? Title { get; init; }
    /// <summary>Описание мероприятия</summary>
    public string? Description { get; init; }
    /// <summary>Дата начала мероприятия</summary>
    [DataType(DataType.DateTime)]
    public DateTimeOffset? StartAt { get; init; }
    /// <summary>Дата окончания мероприятия</summary>
    [DataType(DataType.DateTime)]
    [CompareDates(nameof(StartAt))]
    public DateTimeOffset? EndAt { get; init; }

    /// <summary> Общее количество мест на событии </summary>
    public int? TotalSeats { get; init; }
}
