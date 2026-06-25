using EventsApi.Domain.Entities;

namespace EventsApi.Application.Interfaces
{
	public interface IUserRepository
	{
		/// <summary>
		/// Метод добавления пользователя
		/// </summary>
		/// <param name="user">модель пользователя</param>
		/// <returns></returns>
		Task AddAsync(User user, CancellationToken ct);

		/// <summary>
		/// Проверка существования пользователя при регистрации
		/// </summary>
		/// <param name="login">логин пользователя</param>
		Task<bool> ExistsAsync(string login, CancellationToken ct);


		/// <summary>
		/// Метод получения пользователя по логину
		/// </summary>
		/// <param name="user">модель пользователя</param>
		/// <returns></returns>
		Task<User?> GetUserByLoginAsync(string login, CancellationToken ct);
	}
}
