using EventsApi.Application.DTO.User;
using EventsApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers;

/// <summary>
/// Контроллер для аутентификации пользователей
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController(IAuthenticationService authService ,ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Информация по бронированию
    /// </summary>
    [HttpGet("/auth/register")]
    [Tags("АПИ для работы с пользователями")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest createUserRequest, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Регистрация нового пользователя {NameUser}", nameof(Register), createUserRequest.Login);
        if (await authService.RegisterUserAsync(createUserRequest.Login, createUserRequest.Password, createUserRequest.Role))
            return NoContent();

        return BadRequest(new { message = "Ошибка при регистрации пользователя" });
    }
}