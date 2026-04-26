using System.ComponentModel.DataAnnotations;
using EventsApi.Infrastructure.Attribute;
namespace EventsApi.Models.ModelDTO.Event;
/// <summary>Модель данных для создания мероприятия </summary>
public record CreateEventDTO
{
    /// <summary>Название мероприятия</summary>
    [Required(ErrorMessage = "Название мероприятия обязательно для заполнения")]
    public string Title { get; init; }
    
    /// <summary>Описание мероприятия</summary>
    public string? Description { get; init; }
   
    /// <summary>Дата начала мероприятия</summary>
    [Required(ErrorMessage = "Дата начала мероприятия обязательно для заполнения")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset? StartAt { get; init; }
    
    /// <summary>Дата окончания мероприятия</summary>
    [Required(ErrorMessage = "Дата окончания мероприятия обязательна для заполнения")]
    [DataType(DataType.DateTime)]
    [CompareDates(nameof(StartAt))]
    public DateTimeOffset? EndAt { get; init; }

    /// <summary>общее количество мест на событии</summary>
    [Required(ErrorMessage = "Общее количество мест на событие обязательно для заполнения")]
    [Range(1, 100, ErrorMessage = "Общее количество мест на событие должно быть больше 1 и меньше 100")]
    public int TotalSeats { get; set; }
}
