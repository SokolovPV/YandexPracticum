using System.ComponentModel.DataAnnotations;
using EventsApi.Domain.Enums;
namespace EventsApi.Application.Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class RoleValidationAttribute : ValidationAttribute
{
    private static readonly HashSet<string> roles =
              new HashSet<string>(Enum.GetNames(typeof(RoleType)).Select(name => name.ToLowerInvariant())

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