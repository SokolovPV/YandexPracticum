using System.ComponentModel.DataAnnotations;
using EventsApi.Domain.Enums;
namespace EventsApi.Application.Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class RoleValidationAttribute : ValidationAttribute
{
    private static readonly HashSet<string> roles =
              new HashSet<string>(Enum.GetNames(typeof(RoleType)).Select(name => name.ToLowerInvariant()));

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return new ValidationResult($"Роль не задана");

        if (!roles.Contains(value.ToString()))
        {
            return new ValidationResult($"Недопустимая роль. Допустимые значения: {string.Join(", ", roles)}");
        }
        return ValidationResult.Success;
    }
}