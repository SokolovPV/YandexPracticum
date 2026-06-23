using EventsApi.Application.Attribute;
using System.ComponentModel.DataAnnotations;
namespace EventsApi.Application.DTO.Event;
/// <summary>
/// Модель фильтра для получения событий
/// </summary>
public class EventsFilter
{
    /// <summary>
    /// Название события
    /// </summary>
    [StringLength(100, ErrorMessage = "Название события для поиска длинное")]
    public string? title { get; set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime? from { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [CompareDates(nameof(from))]
    public DateTime? to { get; set; }

    /// <summary>
    /// Страница, которую необходимо вернуть
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
    public int page { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    [Range(1, 100, ErrorMessage = "Размер страницы ограничен 100 элементами")]
    public int pageSize { get; set; } = 10;
}
