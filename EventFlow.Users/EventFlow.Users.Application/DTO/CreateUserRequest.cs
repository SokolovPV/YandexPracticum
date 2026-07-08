using System.ComponentModel.DataAnnotations;
using EventFlow.Users.Application.Attribute;
using EventFlow.Users.Domain.Enums;

namespace EventFlow.Users.Application.DTO;
/// <summary>Модель данных для создания пользователя </summary>
public record CreateUserRequest
{
    /// <summary>Имя входа пользователя</summary>
    [Required(ErrorMessage = "Имя входа пользователя обязательно для заполнения")]
    public required string Login { get; init; }

    /// <summary>Пароль пользователя </summary>
    [Required(ErrorMessage = "Пароль обязателен для заполнения.")]
    public required string Password { get; set; }

    /// <summary>Роль пользователя: 0 - Роль простого пользователя, 1 - Роль администратора</summary>
    [RoleValidation]
    public RoleType Role { get; set; } = RoleType.User;
}