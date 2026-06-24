using System.ComponentModel.DataAnnotations;
using EventsApi.Application.Attribute;
using EventsApi.Domain.Enums;

namespace EventsApi.Application.DTO.User;
/// <summary>Модель данных для создания пользователя </summary>
public record CreateUserRequest
{
    /// <summary>Имя входа пользователя</summary>
    [Required(ErrorMessage = "Имя входа пользователя обязательно для заполнения")]
    public required string Login { get; init; }

    /// <summary>Пароль пользователя </summary>
    [Required(ErrorMessage = "Пароль обязателен для заполнения.")]
    public required string Password { get; set; }

    /// <summary>Роль пользователя</summary>
    [RoleValidation]
    public RoleType Role { get; set; }
}