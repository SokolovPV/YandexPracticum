using System.ComponentModel.DataAnnotations;

namespace EventsApi.Models.ModelDTO
{
  [DateValidation]
  /// <summary>Модель данных для создания мероприятия </summary>
  public record InputEventDTO()
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
    public DateTime? EndAt { get; init; }
  }


  public class DateValidationAttribute : ValidationAttribute
  {
    public override bool IsValid(object? value)
    {
      if (value is InputEventDTO createEventDTO)
      {
        if (createEventDTO.StartAt > createEventDTO.EndAt)
        {
          ErrorMessage = "Дата начала мероприятия больше даты завершения.";
          return false;
        }
        return true;
      }
      return false;
    }
  }
}
