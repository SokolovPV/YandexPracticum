using EventsApi.Domain.Entities;

namespace EventsApi.Application.Interfaces
{
	/// <summary>
	/// репозиторий работы с пользователями
	/// </summary>
	public interface IUserRepository
	{
		/// <summary>
		/// Метод добавления пользователя
		/// </summary>
		/// <param name="user">модель пользователя</param>
		/// <param name="ct">Токен отмены</param>
		Task AddAsync(User user, CancellationToken ct);

		/// <summary>
		/// Проверка существования пользователя при регистрации
		/// </summary>
		/// <param name="login">логин пользователя</param>
		/// <param name="ct">Токен отмены</param>
		Task<bool> ExistsAsync(string login, CancellationToken ct);


		/// <summary>
		/// Метод получения пользователя по логину
		/// </summary>
		/// <param name="login">логин пользователя</param>
		/// <param name="ct">Токен отмены</param>
		Task<User?> GetUserByLoginAsync(string login, CancellationToken ct);

		/// <summary>
		/// Метод получения пользователя по идентификатору
		/// </summary>
		/// <param name="userId">идентификатор пользователя</param>
		/// <param name="ct">Токен отмены</param>
		Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct);
	}
}
