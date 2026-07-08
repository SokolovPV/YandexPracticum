using System.ComponentModel.DataAnnotations;
using EventFlow.Users.Domain.Enums;
namespace EventFlow.Users.Application.Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class RoleValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return new ValidationResult("Роль не указана.");
        
        if (!Enum.IsDefined(typeof(RoleType), value))
        {
            return new ValidationResult($"Недопустимое значение роли. Допустимые значения: {string.Join(", ", Enum.GetNames(typeof(RoleType)))}");
        }
        
        return ValidationResult.Success;
    }
}