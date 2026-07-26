using EventFlow.Users.Domain.Entities;

namespace EventFlow.Users.Application.Interfaces
{
	public interface ITokenGenerator
	{
		/// <summary>
		/// Метод генерации токена
		/// </summary>
		/// <param name="user">модель пользователя</param>
		/// <param name="ct">токен отмены операции (для асинхронности)</param>
		/// <returns></returns>
		String GenerateToken(User user, CancellationToken ct);
	}
}