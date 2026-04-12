using System.ComponentModel.DataAnnotations;
using EventsApi.Infrastructure.Attribute;
namespace EventsApi.Models.ModelDTO.Event;
/// <summary>Модель данных для создания мероприятия </summary>
public record InputEventDTO
{
    /// <summary>Название мероприятия</summary>
    [Required(ErrorMessage = "Название мероприятия обязательно для заполнения")]
    public string Title { get; init; }
    /// <summary>Описание мероприятия</summary>
    public string Description { get; init; }
    /// <summary>Дата начала мероприятия</summary>
    [Required(ErrorMessage = "Дата начала мероприятия обязательно для заполнения")]
    [DataType(DataType.DateTime)]
    public DateTime? StartAt { get; init; }
    /// <summary>Дата окончания мероприятия</summary>
    [Required(ErrorMessage = "Дата окончания мероприятия обязательна для заполнения")]
    [DataType(DataType.DateTime)]
    [CompareDates(nameof(StartAt))]
    public DateTime? EndAt { get; init; }
}
