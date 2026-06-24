
namespace EventsApi.Application.Interfaces
{
	public interface IPasswordHasher
	{
	/// <summary>
	/// метод создания хэш-пароля
	/// </summary>
	/// <param name="password">пароль пользователя</param>
		string HashPassword(string password);

		/// <summary>
		/// метод проверки хэша с паролем
		/// </summary>
		/// <param name="hashedPassword">хэш-пароля</param>
		/// <param name="providedPassword">пароль пользователя</param>
		bool VerifyHashedPassword(string hashedPassword, string providedPassword);
	}
}
