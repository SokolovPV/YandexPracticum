using System.ComponentModel.DataAnnotations;
namespace EventFlow.Events.Application.Attribute;

/// <summary>
/// Атрибут валидации для проверки "Дата окончания должна быть больше даты начала"
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CompareDatesAttribute : ValidationAttribute
{
    public string PropertyName { get; set; }

    public CompareDatesAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var propertyInfo = validationContext.ObjectType.GetProperty(PropertyName);
        if (propertyInfo == null)
            return new ValidationResult($"Свойство {PropertyName} не найдено.");

        var startDateValue = (DateTime?)propertyInfo.GetValue(validationContext.ObjectInstance);
        var endDateValue = (DateTime?)value;
        // Если endDateValue == null, то пропускаем валидацию
        if (!endDateValue.HasValue)
            return ValidationResult.Success!;

        if (startDateValue.HasValue && endDateValue.HasValue && endDateValue <= startDateValue)
            return new ValidationResult("Дата окончания должна быть больше даты начала.");

        return ValidationResult.Success!;
    }
}