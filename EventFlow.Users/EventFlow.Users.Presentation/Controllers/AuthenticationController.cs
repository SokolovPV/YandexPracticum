using EventFlow.Users.Application.DTO;
using EventFlow.Users.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Users.Presentation.Controllers;

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
    /// метод регистрации поьзователя
    /// </summary>
    [HttpPost("/auth/register")]
    [Tags("АПИ для работы с пользователями")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest createUserRequest, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Регистрация нового пользователя {NameUser}", nameof(Register), createUserRequest.Login);
        if (await authService.RegisterUserAsync(createUserRequest.Login, createUserRequest.Password, createUserRequest.Role, ct))
            return NoContent();

        return BadRequest(new { message = "Ошибка при регистрации пользователя" });
    }

	/// <summary>
	/// метод утентификация пользователя и получение JWT токена
	/// </summary>
	[HttpPost("/auth/login")]
	[Tags("АПИ для работы с пользователями")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
	{
		logger.LogDebug("Обработка запроса POST {methodName}. Аутентификация пользователя: {login}", nameof(Login), request.Login);

		var token = await authService.LoginAsync(request.Login!, request.Password!,  ct);
		if (token == null)
			return NotFound(new { message = "Неверные авторизационные данные." });

		return Ok(new { token });
	}
}