using EventsApi.Domain.Entities;

namespace EventsApi.Application.Interfaces
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