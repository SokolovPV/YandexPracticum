using EventFlow.Users.Domain.Entities;

namespace EventFlow.Users.Application.Interfaces
{
	public interface ITokenGenerator
	{
		/// <summary>
		/// Метод генерации токена
		/// </summary>
		/// <param name="user">модель пользователя</param>
		/// <returns></returns>
		String GenerateToken(User user, CancellationToken ct);
	}
}