using System.ComponentModel.DataAnnotations;

namespace EventFlow.Users.Application.DTO
{
	/// <summary>
	/// Данные для аутентификации пользователя
	/// </summary>
	public class LoginRequest
	{
		/// <summary>
		/// Логин пользователя
		/// </summary>
		[Required(ErrorMessage = "Логин обязателен")]
		public string? Login { get; set; }

		/// <summary>
		/// Пароль пользователя
		/// </summary>
		[Required(ErrorMessage = "Пароль обязателен")]
		public string? Password { get; set; }
	}
}
