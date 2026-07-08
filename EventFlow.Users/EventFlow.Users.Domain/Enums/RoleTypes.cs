using System.ComponentModel;

namespace EventFlow.Users.Domain.Enums;
public enum RoleType
{
    /// <summary>
    /// Роль простого пользователя
    /// </summary>
    [Description("Роль простого пользователя")]
    User,
    /// <summary>
    /// Роль администратора
    /// </summary>
    [Description("Роль администратора")]
    Admin
}