using System.ComponentModel.DataAnnotations;
using EventsApi.Infrastructure.Attribute;
namespace EventsApi.Models.ModelDTO.Event;
/// <summary>Модель данных для создания мероприятия </summary>
public record UpdateEventDTO
{
    /// <summary>Название мероприятия</summary>
    public string Title { get; init; }
    /// <summary>Описание мероприятия</summary>
    public string Description { get; init; }
    /// <summary>Дата начала мероприятия</summary>
    [DataType(DataType.DateTime)]
    public DateTime? StartAt { get; init; }
    /// <summary>Дата окончания мероприятия</summary>
    [DataType(DataType.DateTime)]
    [CompareDates(nameof(StartAt))]
    public DateTime? EndAt { get; init; }
}
