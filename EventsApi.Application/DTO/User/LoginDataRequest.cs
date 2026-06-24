using System.ComponentModel.DataAnnotations;

namespace EventsApi.Application.DTO.User
{
	/// <summary>
	/// Данные для аутентификации пользователя
	/// </summary>
	public class LoginDataRequest
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
